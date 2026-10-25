namespace SubTrack.Infrastructure.Persistence;

/// <summary>
/// Registro histórico de una migración ejecutada y persistida en la base de datos.
/// </summary>
public sealed record AppliedMigrationRecord(
    int Version,
    string Name,
    string Checksum,
    DateTimeOffset AppliedAt,
    long DurationMilliseconds);
