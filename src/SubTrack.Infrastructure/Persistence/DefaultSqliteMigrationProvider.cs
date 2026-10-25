namespace SubTrack.Infrastructure.Persistence;

/// <summary>
/// Proveedor oficial de migraciones de SubTrack.Infrastructure.
/// La versión 0001 contiene el esquema de dominio (INF-02B) derivado de las
/// 75 decisiones aprobadas de SubTrack 1.0.
/// </summary>
public sealed class DefaultSqliteMigrationProvider : ISqliteMigrationProvider
{
    public IReadOnlyList<SqliteMigration> GetMigrations() => OfficialMigrations.All;
}
