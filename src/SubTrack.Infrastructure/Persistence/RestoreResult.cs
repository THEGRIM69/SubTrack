namespace SubTrack.Infrastructure.Persistence;

/// <summary>
/// Resultado detallado de una operación de restauración segura de la base SQLite.
/// </summary>
public sealed record RestoreResult(
    string RestoredDatabasePath,
    string SourceBackupPath,
    DateTimeOffset RestoredAt,
    long ApplicationId,
    int UserVersion,
    string? SafetyBackupPath,
    TimeSpan Duration);
