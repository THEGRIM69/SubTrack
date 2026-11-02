namespace SubTrack.Infrastructure.Persistence;

/// <summary>
/// Proveedor de definiciones de migraciones SQLite.
/// </summary>
public interface ISqliteMigrationProvider
{
    /// <summary>
    /// Retorna la lista ordenada de migraciones disponibles.
    /// </summary>
    IReadOnlyList<SqliteMigration> GetMigrations();
}
