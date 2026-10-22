namespace SubTrack.Infrastructure.Persistence;

/// <summary>
/// Opciones de ubicación y comportamiento de la base SQLite local.
/// La ruta debe ser absoluta para evitar depender del directorio de trabajo del proceso.
/// </summary>
public sealed record SubTrackDatabaseOptions
{
    public const string DefaultApplicationFolderName = "SubTrack";
    public const string DefaultDataFolderName = "data";
    public const string DefaultDatabaseFileName = "subtrack.db";

    /// <summary>Ruta absoluta del archivo SQLite.</summary>
    public required string DatabasePath { get; init; }

    /// <summary>Tiempo máximo de espera ante un escritor concurrente.</summary>
    public int BusyTimeoutMilliseconds { get; init; } = 5_000;

    /// <summary>
    /// Ubicación privada por usuario: %LOCALAPPDATA%\SubTrack\data\subtrack.db.
    /// </summary>
    public static SubTrackDatabaseOptions CreateDefault()
    {
        var localApplicationData = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData);

        if (string.IsNullOrWhiteSpace(localApplicationData))
        {
            throw new InvalidOperationException(
                "No se pudo determinar la carpeta LocalApplicationData del usuario.");
        }

        return new SubTrackDatabaseOptions
        {
            DatabasePath = Path.Combine(
                localApplicationData,
                DefaultApplicationFolderName,
                DefaultDataFolderName,
                DefaultDatabaseFileName)
        };
    }

    /// <summary>
    /// Valida y normaliza las opciones. No crea archivos ni directorios.
    /// </summary>
    public SubTrackDatabaseOptions Normalize()
    {
        if (string.IsNullOrWhiteSpace(DatabasePath))
        {
            throw new ArgumentException(
                "La ruta de la base de datos es obligatoria.",
                nameof(DatabasePath));
        }

        if (DatabasePath.Trim().Equals(":memory:", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException(
                "La base de datos en memoria no es compatible con la configuración WAL de SubTrack.");
        }

        if (!Path.IsPathFullyQualified(DatabasePath))
        {
            throw new ArgumentException(
                "La ruta de la base de datos debe ser absoluta.",
                nameof(DatabasePath));
        }

        if (BusyTimeoutMilliseconds is < 0 or > 600_000)
        {
            throw new ArgumentOutOfRangeException(
                nameof(BusyTimeoutMilliseconds),
                "El tiempo de espera debe estar entre 0 y 600000 milisegundos.");
        }

        var fullPath = Path.GetFullPath(DatabasePath);

        if (string.IsNullOrWhiteSpace(Path.GetFileName(fullPath)))
        {
            throw new ArgumentException(
                "La ruta de la base de datos debe incluir un nombre de archivo.",
                nameof(DatabasePath));
        }

        return this with { DatabasePath = fullPath };
    }
}
