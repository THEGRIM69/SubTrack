namespace SubTrack.Infrastructure.Persistence;

/// <summary>
/// Diagnóstico técnico de una base SQLite de SubTrack.
/// No modifica el esquema ni el contenido de la base.
/// </summary>
public sealed record SqliteDatabaseDiagnostics(
    string DatabasePath,
    long ApplicationId,
    int UserVersion,
    bool HasMigrationsTable,
    int MigrationsCount,
    string JournalMode,
    long PageSize,
    long PageCount,
    long FreelistPageCount,
    long DatabaseFileSizeBytes,
    long WalFileSizeBytes,
    long ShmFileSizeBytes)
{
    /// <summary>Indica si la base está operando en modo WAL.</summary>
    public bool IsWalMode =>
        string.Equals(JournalMode, "wal", StringComparison.OrdinalIgnoreCase);
}
