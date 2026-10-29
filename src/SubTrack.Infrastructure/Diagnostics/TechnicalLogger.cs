namespace SubTrack.Infrastructure.Diagnostics;

/// <summary>
/// F0-T23 — Registro técnico local de errores con retención máxima de 30 días.
/// Almacenado fuera de la base de datos financiera, sin datos personales ni envío por red.
/// </summary>
public static class TechnicalLogger
{
    private static readonly object SyncRoot = new();

    public static string LogDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SubTrack",
        "logs");

    public static void LogInfo(string message) => Write("INFO", message, null);

    public static void LogError(string message, Exception? exception = null) => Write("ERROR", message, exception);

    public static void PurgeOldLogs(int retentionDays = 30)
    {
        try
        {
            var dir = LogDirectory;
            if (!Directory.Exists(dir))
            {
                return;
            }

            var cutoff = DateTime.UtcNow.AddDays(-Math.Abs(retentionDays));
            var files = Directory.GetFiles(dir, "subtrack_log_*.log");

            foreach (var file in files)
            {
                try
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.LastWriteTimeUtc < cutoff)
                    {
                        fileInfo.Delete();
                    }
                }
                catch
                {
                    // Ignorar errores individuales de eliminación
                }
            }
        }
        catch
        {
            // La purga nunca debe fallar hacia afuera
        }
    }

    private static void Write(string level, string message, Exception? exception)
    {
        try
        {
            var dir = LogDirectory;
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var logFile = Path.Combine(dir, $"subtrack_log_{DateTime.UtcNow:yyyyMMdd}.log");
            var line = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff UTC}] [{level}] {message}";

            if (exception is not null)
            {
                line += $"{Environment.NewLine}  Excepción: {exception.GetType().FullName}: {exception.Message}{Environment.NewLine}  Pila: {exception.StackTrace}";
            }

            lock (SyncRoot)
            {
                File.AppendAllText(logFile, line + Environment.NewLine);
            }
        }
        catch
        {
            // El logger nunca debe desbordar excepciones al caller
        }
    }
}
