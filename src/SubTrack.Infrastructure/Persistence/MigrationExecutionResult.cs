namespace SubTrack.Infrastructure.Persistence;

/// <summary>
/// Resultado de la ejecución exitosa de una migración individual durante el proceso actual.
/// </summary>
public sealed record MigrationExecutionResult(
    int Version,
    string Name,
    string Checksum,
    TimeSpan Duration);
