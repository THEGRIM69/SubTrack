namespace SubTrack.Infrastructure.Persistence;

/// <summary>
/// Resultado detallado de una operación de respaldo consistente de la base SQLite.
/// </summary>
public sealed record BackupResult(
    string BackupPath,
    DateTimeOffset CreatedAt,
    long FileSizeBytes,
    long ApplicationId,
    int UserVersion,
    TimeSpan Duration,
    string IntegrityCheckResult);
