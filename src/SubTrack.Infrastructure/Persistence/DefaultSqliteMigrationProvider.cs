namespace SubTrack.Infrastructure.Persistence;

/// <summary>
/// Proveedor oficial de migraciones de SubTrack.Infrastructure.
/// En INF-02A no incluye tablas financieras, las cuales corresponden a INF-02B
/// tras la aprobación formal de los contratos de Core.
/// </summary>
public sealed class DefaultSqliteMigrationProvider : ISqliteMigrationProvider
{
    private static readonly IReadOnlyList<SqliteMigration> OfficialMigrations = Array.Empty<SqliteMigration>();

    public IReadOnlyList<SqliteMigration> GetMigrations() => OfficialMigrations;
}
