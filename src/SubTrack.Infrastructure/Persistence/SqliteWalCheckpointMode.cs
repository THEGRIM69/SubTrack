namespace SubTrack.Infrastructure.Persistence;

/// <summary>
/// Modos explícitos de checkpoint WAL soportados por SQLite.
/// </summary>
public enum SqliteWalCheckpointMode
{
    Passive = 0,
    Full = 1,
    Restart = 2,
    Truncate = 3
}
