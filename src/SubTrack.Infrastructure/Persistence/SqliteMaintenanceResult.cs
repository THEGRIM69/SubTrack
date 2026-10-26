namespace SubTrack.Infrastructure.Persistence;

/// <summary>
/// Resultado de una operación de mantenimiento no destructiva.
/// </summary>
public sealed record SqliteMaintenanceResult(
    string Operation,
    bool Success,
    string Details);
