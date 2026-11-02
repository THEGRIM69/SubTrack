using Microsoft.Data.Sqlite;
using SubTrack.Infrastructure.Persistence;

namespace SubTrack.Tests.Infrastructure;

public sealed class SqliteMaintenanceServiceTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly string _databasePath;
    private readonly SubTrackDatabaseOptions _options;

    public SqliteMaintenanceServiceTests()
    {
        _tempDirectory = Path.Combine(
            Path.GetTempPath(),
            "SubTrack.Tests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(_tempDirectory);

        _databasePath = Path.Combine(_tempDirectory, "subtrack-maintenance-test.db");
        _options = new SubTrackDatabaseOptions
        {
            DatabasePath = _databasePath,
            BusyTimeoutMilliseconds = 2_000
        };
    }

    [Fact]
    public async Task GetDiagnosticsAsync_OnMigratedDatabase_ReportsHeaderAndMigrationState()
    {
        await InitializeWithSchemaAsync();
        var service = new SqliteMaintenanceService(_options);

        var diagnostics = await service.GetDiagnosticsAsync();

        Assert.Equal(_databasePath, diagnostics.DatabasePath);
        Assert.Equal(SqliteDatabaseInitializer.ApplicationId, diagnostics.ApplicationId);
        Assert.Equal(1, diagnostics.UserVersion);
        Assert.True(diagnostics.HasMigrationsTable);
        Assert.Equal(1, diagnostics.MigrationsCount);
        Assert.True(diagnostics.IsWalMode);
        Assert.True(diagnostics.PageSize > 0);
        Assert.True(diagnostics.PageCount > 0);
        Assert.True(diagnostics.DatabaseFileSizeBytes > 0);
    }

    [Fact]
    public async Task GetDiagnosticsAsync_BeforeMigrations_ReportsNoMigrationTable()
    {
        var initializer = new SqliteDatabaseInitializer(_options);
        await initializer.InitializeAsync();

        var service = new SqliteMaintenanceService(_options);
        var diagnostics = await service.GetDiagnosticsAsync();

        Assert.False(diagnostics.HasMigrationsTable);
        Assert.Equal(0, diagnostics.MigrationsCount);
        Assert.Equal(0, diagnostics.UserVersion);
    }

    [Fact]
    public async Task CheckIntegrityAsync_OnHealthyDatabase_ReturnsHealthyReport()
    {
        await InitializeWithSchemaAsync();
        await ExecuteSqlAsync(
            "INSERT INTO padre (id) VALUES (1); INSERT INTO hijo (id, padre_id) VALUES (1, 1);");

        var service = new SqliteMaintenanceService(_options);
        var report = await service.CheckIntegrityAsync();

        Assert.True(report.IntegrityOk);
        Assert.True(report.ForeignKeysOk);
        Assert.True(report.IsHealthy);
        Assert.Single(report.IntegrityMessages);
        Assert.Equal("ok", report.IntegrityMessages[0], ignoreCase: true);
        Assert.Empty(report.ForeignKeyViolations);
        Assert.Equal(1, report.Diagnostics.UserVersion);
        Assert.Equal(1, report.Diagnostics.MigrationsCount);
    }

    [Fact]
    public async Task CheckIntegrityAsync_WithOrphanForeignKey_ReturnsViolation()
    {
        await InitializeWithSchemaAsync();

        await using (var connection = new SqliteConnection($"Data Source={_databasePath}"))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText =
                "PRAGMA foreign_keys = OFF; INSERT INTO hijo (id, padre_id) VALUES (1, 99);";
            await command.ExecuteNonQueryAsync();
        }

        var service = new SqliteMaintenanceService(_options);
        var report = await service.CheckIntegrityAsync();

        Assert.True(report.IntegrityOk);
        Assert.False(report.ForeignKeysOk);
        Assert.False(report.IsHealthy);

        var violation = Assert.Single(report.ForeignKeyViolations);
        Assert.Equal("hijo", violation.Table);
        Assert.Equal(1L, violation.RowId);
        Assert.Equal("padre", violation.ParentTable);
        Assert.Equal(0, violation.ForeignKeyIndex);
    }

    [Fact]
    public async Task GetDiagnosticsAsync_WhenDatabaseDoesNotExist_ThrowsCannotOpenAndDoesNotCreateFile()
    {
        var service = new SqliteMaintenanceService(_options);

        var exception = await Assert.ThrowsAsync<SubTrackPersistenceException>(
            () => service.GetDiagnosticsAsync());

        Assert.Equal(PersistenceFailureKind.CannotOpen, exception.Kind);
        Assert.False(File.Exists(_databasePath));
    }

    [Fact]
    public async Task CheckIntegrityAsync_WhenDatabaseBelongsToAnotherApplication_ThrowsForeignDatabase()
    {
        await CreateForeignDatabaseAsync();
        var service = new SqliteMaintenanceService(_options);

        var exception = await Assert.ThrowsAsync<SubTrackPersistenceException>(
            () => service.CheckIntegrityAsync());

        Assert.Equal(PersistenceFailureKind.ForeignDatabase, exception.Kind);
    }

    [Fact]
    public async Task GetDiagnosticsAsync_WhenFileIsNotSqlite_ThrowsNotADatabase()
    {
        await File.WriteAllTextAsync(_databasePath, "esto no es una base de datos SQLite");
        var service = new SqliteMaintenanceService(_options);

        var exception = await Assert.ThrowsAsync<SubTrackPersistenceException>(
            () => service.GetDiagnosticsAsync());

        Assert.Equal(PersistenceFailureKind.NotADatabase, exception.Kind);
    }

    [Fact]
    public async Task OptimizeAsync_OnMigratedDatabase_ReturnsSuccess()
    {
        await InitializeWithSchemaAsync();
        var service = new SqliteMaintenanceService(_options);

        var result = await service.OptimizeAsync();

        Assert.True(result.Success);
        Assert.Equal("optimize", result.Operation);
    }

    [Fact]
    public async Task CheckpointWalAsync_OnWalDatabase_ExecutesCheckpoint()
    {
        await InitializeWithSchemaAsync();
        SqliteConnection.ClearAllPools();

        var service = new SqliteMaintenanceService(_options);
        var result = await service.CheckpointWalAsync(SqliteWalCheckpointMode.Truncate);

        Assert.True(result.Executed);
        Assert.Equal(0, result.Busy);
        Assert.True(result.Completed);
        Assert.True(result.LogPages >= -1);
        Assert.True(result.CheckpointedPages >= -1);
    }

    [Fact]
    public async Task CheckpointWalAsync_WhenDatabaseIsNotWal_DoesNotExecuteCheckpoint()
    {
        await CreateNonWalDatabaseAsync();
        var service = new SqliteMaintenanceService(_options);

        var result = await service.CheckpointWalAsync(SqliteWalCheckpointMode.Passive);

        Assert.False(result.Executed);
        Assert.False(result.Completed);
        Assert.True(result.Details.Contains("WAL", StringComparison.OrdinalIgnoreCase));
    }

    private async Task InitializeWithSchemaAsync()
    {
        var initializer = new SqliteDatabaseInitializer(_options);
        await initializer.InitializeAsync();

        var migrations = new[]
        {
            new SqliteMigration(
                1,
                "crear_esquema_prueba",
                """
                CREATE TABLE padre (id INTEGER PRIMARY KEY);
                CREATE TABLE hijo (
                    id INTEGER PRIMARY KEY,
                    padre_id INTEGER NOT NULL REFERENCES padre(id)
                );
                """)
        };

        var migrator = new SqliteDatabaseMigrator(
            _options,
            new StaticSqliteMigrationProvider(migrations));

        await migrator.MigrateAsync();
    }

    private async Task ExecuteSqlAsync(string sql)
    {
        await using var connection = new SqliteConnection($"Data Source={_databasePath}");
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    private async Task CreateForeignDatabaseAsync()
    {
        await using var connection = new SqliteConnection($"Data Source={_databasePath}");
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText =
            "PRAGMA application_id = 123; CREATE TABLE otra (id INTEGER PRIMARY KEY);";
        await command.ExecuteNonQueryAsync();
    }

    private async Task CreateNonWalDatabaseAsync()
    {
        await using var connection = new SqliteConnection($"Data Source={_databasePath}");
        await connection.OpenAsync();

        await using (var identityCommand = connection.CreateCommand())
        {
            identityCommand.CommandText =
                $"PRAGMA application_id = {SqliteDatabaseInitializer.ApplicationId};";
            await identityCommand.ExecuteNonQueryAsync();
        }

        await using (var journalCommand = connection.CreateCommand())
        {
            journalCommand.CommandText = "PRAGMA journal_mode = DELETE;";
            await journalCommand.ExecuteScalarAsync();
        }

        await using (var tableCommand = connection.CreateCommand())
        {
            tableCommand.CommandText = "CREATE TABLE prueba (id INTEGER PRIMARY KEY);";
            await tableCommand.ExecuteNonQueryAsync();
        }
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
