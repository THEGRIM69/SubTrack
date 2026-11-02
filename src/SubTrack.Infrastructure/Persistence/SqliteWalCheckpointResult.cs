namespace SubTrack.Infrastructure.Persistence;

/// <summary>
/// Resultado de un checkpoint WAL explícito.
/// </summary>
public sealed record SqliteWalCheckpointResult(
    SqliteWalCheckpointMode Mode,
    bool Executed,
    int Busy,
    int LogPages,
    int CheckpointedPages,
    string Details)
{
    /// <summary>Indica si el checkpoint se ejecutó y no quedó bloqueado por lectores o escritores.</summary>
    public bool Completed => Executed && Busy == 0;
}
