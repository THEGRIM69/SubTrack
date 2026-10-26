namespace SubTrack.Infrastructure.Persistence;

/// <summary>
/// Información resumida de un archivo de respaldo almacenado en disco.
/// </summary>
public sealed record BackupInfo(
    string FilePath,
    string FileName,
    DateTimeOffset CreatedAt,
    long FileSizeBytes);
