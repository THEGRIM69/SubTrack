namespace SubTrack.Infrastructure.Persistence;

/// <summary>
/// Proveedor de migraciones en memoria o estáticas, utilizado para configuración
/// de pruebas o inyección personalizada de scripts.
/// </summary>
public sealed class StaticSqliteMigrationProvider : ISqliteMigrationProvider
{
    private readonly IReadOnlyList<SqliteMigration> _migrations;

    public StaticSqliteMigrationProvider(IEnumerable<SqliteMigration> migrations)
    {
        ArgumentNullException.ThrowIfNull(migrations);
        _migrations = migrations.OrderBy(m => m.Version).ToList();
    }

    public IReadOnlyList<SqliteMigration> GetMigrations() => _migrations;
}
