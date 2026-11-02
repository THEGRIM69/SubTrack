namespace SubTrack.Infrastructure.Persistence;

/// <summary>
/// Resultado verificable de la inicialización de la base SQLite.
/// </summary>
public sealed record DatabaseInitializationResult(
    string DatabasePath,
    string JournalMode,
    bool ForeignKeysEnabled,
    string SynchronousMode,
    long ApplicationId,
    int BusyTimeoutMilliseconds,
    string IntegrityCheck);
