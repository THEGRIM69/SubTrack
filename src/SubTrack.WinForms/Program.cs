using SubTrack.Infrastructure;
using SubTrack.Infrastructure.Diagnostics;
using SubTrack.Infrastructure.Persistence;
using System.Security.Cryptography;
using System.Text;

namespace SubTrack.WinForms;

static class Program
{
    private const string SingleInstanceMutexName = "SubTrack_SingleInstance_Mutex_v1";

    /// <summary>
    /// F9 — Arranque: preparar SQLite, migrar y abrir la ventana principal.
    /// F0-T20: Instancia única de la aplicación.
    /// F0-T23: Registro técnico local de errores con purga a 30 días.
    /// F0-16, F0-T16: Respaldo automático diario preventivo.
    /// </summary>
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        SubTrackDatabaseOptions options;
        try
        {
            options = ResolveDatabaseOptions(args);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException)
        {
            MessageBox.Show(
                exception.Message,
                "SubTrack — argumento inválido",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        var mutexName = string.Equals(
            options.DatabasePath,
            SubTrackDatabaseOptions.CreateDefault().DatabasePath,
            StringComparison.OrdinalIgnoreCase)
            ? SingleInstanceMutexName
            : $"{SingleInstanceMutexName}_{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(options.DatabasePath)))}";

        using var mutex = new Mutex(true, mutexName, out bool isNewInstance);
        if (!isNewInstance)
        {
            MessageBox.Show(
                "SubTrack ya se encuentra en ejecución en este equipo.",
                "SubTrack",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        TechnicalLogger.PurgeOldLogs(30);

        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, args) =>
        {
            TechnicalLogger.LogError("Error no controlado en hilo de interfaz.", args.Exception);
            MessageBox.Show(
                $"Error inesperado:{Environment.NewLine}{args.Exception.Message}",
                "SubTrack",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception ex)
            {
                TechnicalLogger.LogError("Error crítico fatal en AppDomain.", ex);
            }
        };

        try
        {
            new SqliteDatabaseInitializer(options)
                .InitializeAsync()
                .GetAwaiter()
                .GetResult();

            var migration = new SqliteDatabaseMigrator(options)
                .MigrateAsync()
                .GetAwaiter()
                .GetResult();

            if (!migration.Success)
            {
                TechnicalLogger.LogError("No se pudo aplicar las migraciones a la base de datos.");
                MessageBox.Show(
                    "No se pudo preparar la base de datos.",
                    "SubTrack",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return;
            }

            // F0-16, F0-T16: Respaldo automático diario al arranque
            TriggerDailyAutoBackupAsync(options);

            var services = new SubTrackServices(options);

            Application.Run(new MainForm(services, options));
        }
        catch (Exception exception)
        {
            TechnicalLogger.LogError("Fallo catastrófico al iniciar SubTrack.", exception);
            MessageBox.Show(
                $"No se pudo iniciar SubTrack:{Environment.NewLine}{exception.Message}",
                "SubTrack",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private static SubTrackDatabaseOptions ResolveDatabaseOptions(string[] args)
    {
        if (args.Length == 0)
        {
            return SubTrackDatabaseOptions.CreateDefault();
        }

        if (args.Length != 2 || !string.Equals(args[0], "--database", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Uso: SubTrack.WinForms.exe [--database <ruta-absoluta>].");
        }

        if (string.IsNullOrWhiteSpace(args[1]))
        {
            throw new ArgumentException("Debe indicar una ruta absoluta para la base SQLite.");
        }

        var selected = SubTrackDatabaseOptions.CreateForDatabasePath(args[1]);
        var production = SubTrackDatabaseOptions.CreateDefault();

        if (string.Equals(selected.DatabasePath, production.DatabasePath, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "La ruta indicada coincide con la base de producción. Elija una ruta SQLite de prueba independiente.");
        }

        return selected;
    }

    private static void TriggerDailyAutoBackupAsync(SubTrackDatabaseOptions options)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var backupService = new SqliteBackupService(options);
                var todayPattern = $"subtrack_backup_{DateTime.UtcNow:yyyyMMdd}";
                var backups = await backupService.ListBackupsAsync().ConfigureAwait(false);

                if (!backups.Any(b => b.FileName.StartsWith(todayPattern, StringComparison.OrdinalIgnoreCase)))
                {
                    await backupService.CreateBackupAsync().ConfigureAwait(false);
                    TechnicalLogger.LogInfo($"Respaldo automático diario creado para {DateTime.UtcNow:yyyy-MM-dd}.");
                }

                await backupService.PruneBackupsAsync(7).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                TechnicalLogger.LogError("Error durante la rutina de respaldo diario automático al inicio.", ex);
            }
        });
    }
}
