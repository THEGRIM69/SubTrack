using Microsoft.Data.Sqlite;
using SubTrack.Infrastructure.Persistence;

namespace SubTrack.Tests.Infrastructure;

public sealed class SqliteDatabaseMigratorTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly string _databasePath;
    private readonly SubTrackDatabaseOptions _options;

    public SqliteDatabaseMigratorTests()
    {
        _tempDirectory = Path.Combine(
            Path.GetTempPath(),
            "SubTrack.Tests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(_tempDirectory);

        _databasePath = Path.Combine(_tempDirectory, "subtrack-migrator-test.db");
        _options = new SubTrackDatabaseOptions
        {
            DatabasePath = _databasePath,
            BusyTimeoutMilliseconds = 2_000
        };
    }

    [Fact]
    public async Task MigrateAsync_WithDefaultProvider_EnsuresMigrationTableAndReturnsZeroApplied()
    {
        var migrator = new SqliteDatabaseMigrator(_options);

        var result = await migrator.MigrateAsync();

        Assert.True(result.Success);
        Assert.True(result.IsUpToDate);
        Assert.Equal(0, result.InitialVersion);
        Assert.Equal(0, result.FinalVersion);
        Assert.Empty(result.AppliedMigrations);

        var currentVersion = await migrator.GetCurrentVersionAsync();
        Assert.Equal(0, currentVersion);

        var history = await migrator.GetAppliedMigrationsAsync();
        Assert.Empty(history);
    }

    [Fact]
    public async Task MigrateAsync_WithValidMigrations_ExecutesScriptsSequentiallyAndUpdatesUserVersion()
    {
        var migrations = new[]
        {
            new SqliteMigration(1, "crear_tabla_prueba_a", "CREATE TABLE prueba_a (id INTEGER PRIMARY KEY, valor TEXT);"),
            new SqliteMigration(2, "crear_tabla_prueba_b", "CREATE TABLE prueba_b (id INTEGER PRIMARY KEY, nombre TEXT);")
        };

        var provider = new StaticSqliteMigrationProvider(migrations);
        var migrator = new SqliteDatabaseMigrator(_options, provider);

        var result = await migrator.MigrateAsync();

        Assert.True(result.Success);
        Assert.False(result.IsUpToDate);
        Assert.Equal(0, result.InitialVersion);
        Assert.Equal(2, result.FinalVersion);
        Assert.Equal(2, result.AppliedMigrations.Count);

        var currentVersion = await migrator.GetCurrentVersionAsync();
        Assert.Equal(2, currentVersion);

        // Verificar que las tablas existen
        await using var connection = new SqliteConnection($"Data Source={_databasePath}");
        await connection.OpenAsync();

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name IN ('prueba_a', 'prueba_b');";
        var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        Assert.Equal(2, count);

        // Verificar user_version en header SQLite
        await using var userVersionCmd = connection.CreateCommand();
        userVersionCmd.CommandText = "PRAGMA user_version;";
        var userVer = Convert.ToInt32(await userVersionCmd.ExecuteScalarAsync());
        Assert.Equal(2, userVer);

        // Verificar historial
        var history = await migrator.GetAppliedMigrationsAsync();
        Assert.Equal(2, history.Count);
        Assert.Equal(1, history[0].Version);
        Assert.Equal("crear_tabla_prueba_a", history[0].Name);
        Assert.Equal(migrations[0].CalculateChecksum(), history[0].Checksum);
        Assert.Equal(2, history[1].Version);
        Assert.Equal("crear_tabla_prueba_b", history[1].Name);
        Assert.Equal(migrations[1].CalculateChecksum(), history[1].Checksum);
    }

    [Fact]
    public async Task MigrateAsync_WhenAlreadyApplied_IsIdempotentAndDoesNotReapply()
    {
        var migrations = new[]
        {
            new SqliteMigration(1, "crear_tabla_idempotente", "CREATE TABLE idempotente (id INTEGER PRIMARY KEY);")
        };

        var provider = new StaticSqliteMigrationProvider(migrations);
        var migrator = new SqliteDatabaseMigrator(_options, provider);

        var firstResult = await migrator.MigrateAsync();
        Assert.Single(firstResult.AppliedMigrations);

        var secondResult = await migrator.MigrateAsync();
        Assert.True(secondResult.Success);
        Assert.True(secondResult.IsUpToDate);
        Assert.Empty(secondResult.AppliedMigrations);
        Assert.Equal(1, secondResult.InitialVersion);
        Assert.Equal(1, secondResult.FinalVersion);

        var history = await migrator.GetAppliedMigrationsAsync();
        Assert.Single(history);
    }

    [Fact]
    public async Task MigrateAsync_WhenUserVersionIsBehindAppliedHistory_SynchronizesUserVersion()
    {
        var migrations = new[]
        {
            new SqliteMigration(1, "v1", "CREATE TABLE v1 (id INTEGER PRIMARY KEY);")
        };

        var provider = new StaticSqliteMigrationProvider(migrations);
        var migrator = new SqliteDatabaseMigrator(_options, provider);
        await migrator.MigrateAsync();

        // Simular una base cuyo header quedó atrasado respecto del historial aplicado.
        await using (var connection = new SqliteConnection($"Data Source={_databasePath}"))
        {
            await connection.OpenAsync();
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "PRAGMA user_version = 0;";
            await cmd.ExecuteNonQueryAsync();
        }

        var result = await migrator.MigrateAsync();

        Assert.True(result.IsUpToDate);
        Assert.Empty(result.AppliedMigrations);

        await using var checkConnection = new SqliteConnection($"Data Source={_databasePath}");
        await checkConnection.OpenAsync();
        await using var checkCmd = checkConnection.CreateCommand();
        checkCmd.CommandText = "PRAGMA user_version;";
        var userVersion = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
        Assert.Equal(1, userVersion);
    }

    [Fact]
    public async Task MigrateAsync_WhenChecksumMismatch_ThrowsSubTrackPersistenceExceptionWithChecksumMismatchKind()
    {
        var initialMigration = new SqliteMigration(
            1,
            "tabla_modificada",
            "CREATE TABLE original (id INTEGER PRIMARY KEY);");

        var initialProvider = new StaticSqliteMigrationProvider(new[] { initialMigration });
        var firstMigrator = new SqliteDatabaseMigrator(_options, initialProvider);
        await firstMigrator.MigrateAsync();

        // Modificamos el contenido del script de la versión 1 (alteración post-aplicación)
        var alteredMigration = new SqliteMigration(
            1,
            "tabla_modificada",
            "CREATE TABLE original (id INTEGER PRIMARY KEY, columna_modificada TEXT);");

        var alteredProvider = new StaticSqliteMigrationProvider(new[] { alteredMigration });
        var secondMigrator = new SqliteDatabaseMigrator(_options, alteredProvider);

        var exception = await Assert.ThrowsAsync<SubTrackPersistenceException>(
            () => secondMigrator.MigrateAsync());

        Assert.Equal(PersistenceFailureKind.ChecksumMismatch, exception.Kind);
        Assert.Contains("checksum", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MigrateAsync_WhenDatabaseHasFutureVersion_ThrowsSubTrackPersistenceExceptionWithFutureVersionKind()
    {
        var migrations = new[]
        {
            new SqliteMigration(1, "v1", "CREATE TABLE v1 (id INTEGER PRIMARY KEY);"),
            new SqliteMigration(2, "v2", "CREATE TABLE v2 (id INTEGER PRIMARY KEY);")
        };

        var fullProvider = new StaticSqliteMigrationProvider(migrations);
        var initialMigrator = new SqliteDatabaseMigrator(_options, fullProvider);
        await initialMigrator.MigrateAsync();

        // Simulamos una versión de la app antigua que solo conoce hasta la versión 1
        var legacyMigrations = new[]
        {
            new SqliteMigration(1, "v1", "CREATE TABLE v1 (id INTEGER PRIMARY KEY);")
        };

        var legacyProvider = new StaticSqliteMigrationProvider(legacyMigrations);
        var legacyMigrator = new SqliteDatabaseMigrator(_options, legacyProvider);

        var exception = await Assert.ThrowsAsync<SubTrackPersistenceException>(
            () => legacyMigrator.MigrateAsync());

        Assert.Equal(PersistenceFailureKind.FutureVersion, exception.Kind);
        Assert.Contains("superior a la versión máxima", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MigrateAsync_WhenMigrationScriptFails_RollsBackTransactionAndLeavesPreviousVersionIntact()
    {
        var migrations = new[]
        {
            new SqliteMigration(1, "v1_valida", "CREATE TABLE exito (id INTEGER PRIMARY KEY);"),
            new SqliteMigration(2, "v2_invalida", "CREATE TABLE fallo (id INTEGER PRIMARY KEY); SINTAXIS_SQL_INVALIDA_TOTAL;")
        };

        var provider = new StaticSqliteMigrationProvider(migrations);
        var migrator = new SqliteDatabaseMigrator(_options, provider);

        var exception = await Assert.ThrowsAsync<SubTrackPersistenceException>(
            () => migrator.MigrateAsync());

        Assert.Equal(PersistenceFailureKind.MigrationFailed, exception.Kind);
        Assert.Contains("v2_invalida", exception.Message);

        // Verificar que la versión se quedó en 1
        var currentVersion = await migrator.GetCurrentVersionAsync();
        Assert.Equal(1, currentVersion);

        // Verificar que la tabla 'fallo' NO fue creada debido al rollback
        await using var connection = new SqliteConnection($"Data Source={_databasePath}");
        await connection.OpenAsync();

        await using var checkCmd = connection.CreateCommand();
        checkCmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='fallo';";
        var falloCount = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
        Assert.Equal(0, falloCount);

        // La tabla 'exito' sí debe existir
        checkCmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='exito';";
        var exitoCount = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
        Assert.Equal(1, exitoCount);
    }

    [Fact]
    public async Task MigrateAsync_WhenSequenceHasGap_ThrowsSubTrackPersistenceException()
    {
        var migrations = new[]
        {
            new SqliteMigration(1, "v1", "CREATE TABLE v1 (id INTEGER PRIMARY KEY);"),
            new SqliteMigration(3, "v3_con_hueco", "CREATE TABLE v3 (id INTEGER PRIMARY KEY);") // falta v2
        };

        var provider = new StaticSqliteMigrationProvider(migrations);
        var migrator = new SqliteDatabaseMigrator(_options, provider);

        // Aplicamos v1
        var v1Provider = new StaticSqliteMigrationProvider(new[] { migrations[0] });
        var firstMigrator = new SqliteDatabaseMigrator(_options, v1Provider);
        await firstMigrator.MigrateAsync();

        // Ahora intentamos aplicar v3 directamente teniendo v1
        var exception = await Assert.ThrowsAsync<SubTrackPersistenceException>(
            () => migrator.MigrateAsync());

        Assert.Equal(PersistenceFailureKind.MigrationFailed, exception.Kind);
        Assert.Contains("discontinua", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MigrateAsync_OnForeignDatabase_ThrowsSubTrackPersistenceExceptionWithForeignDatabaseKind()
    {
        await using (var connection = new SqliteConnection($"Data Source={_databasePath}"))
        {
            await connection.OpenAsync();
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "PRAGMA application_id = 999999;";
            await cmd.ExecuteNonQueryAsync();
        }

        var migrator = new SqliteDatabaseMigrator(_options);

        var exception = await Assert.ThrowsAsync<SubTrackPersistenceException>(
            () => migrator.MigrateAsync());

        Assert.Equal(PersistenceFailureKind.ForeignDatabase, exception.Kind);
    }

    [Fact]
    public async Task MigrateAsync_WhenPendingSequenceHasGapInMiddle_ThrowsSubTrackPersistenceException()
    {
        var migrations = new[]
        {
            new SqliteMigration(1, "v1", "CREATE TABLE v1 (id INTEGER PRIMARY KEY);"),
            new SqliteMigration(3, "v3_con_hueco", "CREATE TABLE v3 (id INTEGER PRIMARY KEY);")
        };

        var provider = new StaticSqliteMigrationProvider(migrations);
        var migrator = new SqliteDatabaseMigrator(_options, provider);

        var exception = await Assert.ThrowsAsync<SubTrackPersistenceException>(
            () => migrator.MigrateAsync());

        Assert.Equal(PersistenceFailureKind.MigrationFailed, exception.Kind);
        Assert.Contains("discontinua", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MigrateAsync_WhenAppliedHistoryHasGap_ThrowsSubTrackPersistenceException()
    {
        var v1 = new SqliteMigration(1, "v1", "CREATE TABLE v1 (id INTEGER PRIMARY KEY);");
        var v2 = new SqliteMigration(2, "v2", "CREATE TABLE v2 (id INTEGER PRIMARY KEY);");
        var v3 = new SqliteMigration(3, "v3", "CREATE TABLE v3 (id INTEGER PRIMARY KEY);");

        // Creamos la tabla de migraciones manualmente e insertamos v1 y v3 (omitiendo v2)
        await using (var connection = new SqliteConnection($"Data Source={_databasePath}"))
        {
            await connection.OpenAsync();
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = $@"
                PRAGMA application_id = {SqliteDatabaseMigrator.ApplicationId};
                CREATE TABLE {SqliteDatabaseMigrator.MigrationsTableName} (
                    version INTEGER NOT NULL PRIMARY KEY,
                    nombre TEXT NOT NULL,
                    checksum TEXT NOT NULL,
                    aplicada_en TEXT NOT NULL,
                    duracion_ms INTEGER NOT NULL
                );
                INSERT INTO {SqliteDatabaseMigrator.MigrationsTableName} VALUES
                    (1, 'v1', '{v1.CalculateChecksum()}', '2026-10-23T00:00:00Z', 10),
                    (3, 'v3', '{v3.CalculateChecksum()}', '2026-10-23T00:00:00Z', 10);";
            await cmd.ExecuteNonQueryAsync();
        }

        var provider = new StaticSqliteMigrationProvider(new[] { v1, v2, v3 });
        var migrator = new SqliteDatabaseMigrator(_options, provider);

        var exception = await Assert.ThrowsAsync<SubTrackPersistenceException>(
            () => migrator.MigrateAsync());

        Assert.Equal(PersistenceFailureKind.MigrationFailed, exception.Kind);
        Assert.Contains("no figura como aplicada", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MigrateAsync_WhenCatalogHasDuplicateVersions_ThrowsSubTrackPersistenceException()
    {
        var migrations = new[]
        {
            new SqliteMigration(1, "v1_a", "CREATE TABLE a (id INTEGER PRIMARY KEY);"),
            new SqliteMigration(1, "v1_b", "CREATE TABLE b (id INTEGER PRIMARY KEY);")
        };

        var provider = new StaticSqliteMigrationProvider(migrations);
        var migrator = new SqliteDatabaseMigrator(_options, provider);

        var exception = await Assert.ThrowsAsync<SubTrackPersistenceException>(
            () => migrator.MigrateAsync());

        Assert.Equal(PersistenceFailureKind.MigrationFailed, exception.Kind);
        Assert.Contains("Existe más de una migración", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SqliteMigration_Constructor_ValidatesParameters()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SqliteMigration(0, "nombre", "SQL;"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SqliteMigration(-1, "nombre", "SQL;"));
        Assert.Throws<ArgumentException>(() => new SqliteMigration(1, "", "SQL;"));
        Assert.Throws<ArgumentException>(() => new SqliteMigration(1, "   ", "SQL;"));
        Assert.Throws<ArgumentException>(() => new SqliteMigration(1, "nombre", ""));
        Assert.Throws<ArgumentException>(() => new SqliteMigration(1, "nombre", "   "));
    }

    [Fact]
    public void SqliteMigration_CalculateChecksum_NormalizesDifferentNewlinesIdentically()
    {
        var migrationCrlf = new SqliteMigration(1, "test", "CREATE TABLE t (id INT);\r\nINSERT INTO t VALUES (1);\r\n");
        var migrationLf = new SqliteMigration(1, "test", "CREATE TABLE t (id INT);\nINSERT INTO t VALUES (1);\n");
        var migrationCr = new SqliteMigration(1, "test", "CREATE TABLE t (id INT);\rINSERT INTO t VALUES (1);\r");

        var checksumCrlf = migrationCrlf.CalculateChecksum();
        var checksumLf = migrationLf.CalculateChecksum();
        var checksumCr = migrationCr.CalculateChecksum();

        Assert.Equal(checksumLf, checksumCrlf);
        Assert.Equal(checksumLf, checksumCr);
        Assert.Equal(64, checksumLf.Length);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();

        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                if (Directory.Exists(_tempDirectory))
                {
                    Directory.Delete(_tempDirectory, recursive: true);
                }

                return;
            }
            catch (IOException)
            {
                Thread.Sleep(50);
            }
            catch (UnauthorizedAccessException)
            {
                Thread.Sleep(50);
            }
        }
    }
}
