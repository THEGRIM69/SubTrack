namespace SubTrack.Infrastructure.Persistence;

/// <summary>
/// Resultado de la verificación estructural y de integridad de un archivo de respaldo.
/// </summary>
public sealed record BackupVerificationResult(
    bool IsValid,
    string BackupPath,
    long FileSizeBytes,
    long ApplicationId,
    int UserVersion,
    string IntegrityStatus,
    string? FailureReason,
    int MigrationsCount);
