namespace SubTrack.Infrastructure.Persistence;

/// <summary>
/// Opciones de configuración para la generación y retención de respaldos SQLite.
/// </summary>
public sealed record SubTrackBackupOptions
{
    public const string DefaultBackupFolderName = "backups";
    public const string DefaultBackupPrefix = "subtrack_backup";

    /// <summary>Directorio absoluto donde se depositan los respaldos.</summary>
    public string? BackupDirectoryPath { get; init; }

    /// <summary>Prefijo para el nombre de los archivos de respaldo generados.</summary>
    public string BackupFilePrefix { get; init; } = DefaultBackupPrefix;

    /// <summary>Número predeterminado de respaldos a conservar en operaciones de poda.</summary>
    public int DefaultRetentionCount { get; init; } = 30;

    /// <summary>
    /// Genera la configuración predeterminada ubicada bajo %LOCALAPPDATA%\SubTrack\backups.
    /// </summary>
    public static SubTrackBackupOptions CreateDefault(SubTrackDatabaseOptions databaseOptions)
    {
        ArgumentNullException.ThrowIfNull(databaseOptions);

        var dataDirectory = Path.GetDirectoryName(databaseOptions.Normalize().DatabasePath);
        var baseDirectory = Path.GetDirectoryName(dataDirectory);

        var backupDirectory = !string.IsNullOrWhiteSpace(baseDirectory)
            ? Path.Combine(baseDirectory, DefaultBackupFolderName)
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SubTrack", DefaultBackupFolderName);

        return new SubTrackBackupOptions
        {
            BackupDirectoryPath = backupDirectory
        };
    }

    public SubTrackBackupOptions Normalize(SubTrackDatabaseOptions databaseOptions)
    {
        ArgumentNullException.ThrowIfNull(databaseOptions);

        var backupDir = BackupDirectoryPath;
        if (string.IsNullOrWhiteSpace(backupDir))
        {
            var dataDirectory = Path.GetDirectoryName(databaseOptions.Normalize().DatabasePath);
            var baseDirectory = Path.GetDirectoryName(dataDirectory);
            backupDir = !string.IsNullOrWhiteSpace(baseDirectory)
                ? Path.Combine(baseDirectory, DefaultBackupFolderName)
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SubTrack", DefaultBackupFolderName);
        }

        var fullPath = Path.GetFullPath(backupDir);

        return this with
        {
            BackupDirectoryPath = fullPath,
            BackupFilePrefix = string.IsNullOrWhiteSpace(BackupFilePrefix) ? DefaultBackupPrefix : BackupFilePrefix.Trim()
        };
    }
}
