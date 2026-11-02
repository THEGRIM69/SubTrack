using Microsoft.Data.Sqlite;
using SubTrack.Infrastructure.Persistence;

namespace SubTrack.Tests.Infrastructure;

public sealed class SqliteDatabaseInitializerTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly string _databasePath;
    private readonly SubTrackDatabaseOptions _options;

    public SqliteDatabaseInitializerTests()
    {
        _tempDirectory = Path.Combine(
            Path.GetTempPath(),
            "SubTrack.Tests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(_tempDirectory);

        _databasePath = Path.Combine(_tempDirectory, "subtrack-test.db");
        _options = new SubTrackDatabaseOptions
        {
            DatabasePath = _databasePath,
            BusyTimeoutMilliseconds = 2_000
        };
    }

    [Fact]
    public async Task InitializeAsync_CreatesDatabaseAndAppliesPragmas()
    {
        var initializer = new SqliteDatabaseInitializer(_options);

        var result = await initializer.InitializeAsync();

        Assert.Equal(Path.GetFullPath(_databasePath), result.DatabasePath);
        Assert.Equal("wal", result.JournalMode, ignoreCase: true);
        Assert.True(result.ForeignKeysEnabled);
        Assert.Equal("FULL", result.SynchronousMode);
        Assert.Equal(SqliteDatabaseInitializer.ApplicationId, result.ApplicationId);
        Assert.Equal(2_000, result.BusyTimeoutMilliseconds);
        Assert.Equal("ok", result.IntegrityCheck, ignoreCase: true);
        Assert.True(File.Exists(_databasePath));
    }

    [Fact]
    public async Task InitializeAsync_IsIdempotent()
    {
        var initializer = new SqliteDatabaseInitializer(_options);

        var first = await initializer.InitializeAsync();
        var second = await initializer.InitializeAsync();

        Assert.Equal(first.DatabasePath, second.DatabasePath);
        Assert.Equal(first.ApplicationId, second.ApplicationId);
        Assert.Equal(first.IntegrityCheck, second.IntegrityCheck);
    }

    [Fact]
    public async Task InitializeAsync_CreatesMissingDataDirectory()
    {
        var nestedPath = Path.Combine(_tempDirectory, "nested", "data", "subtrack.db");
        var options = new SubTrackDatabaseOptions
        {
            DatabasePath = nestedPath,
            BusyTimeoutMilliseconds = 2_000
        };

        var initializer = new SqliteDatabaseInitializer(options);
        var result = await initializer.InitializeAsync();

        Assert.True(Directory.Exists(Path.GetDirectoryName(nestedPath)));
        Assert.True(File.Exists(result.DatabasePath));
    }

    [Fact]
    public async Task InitializeAsync_DoesNotCreateDomainSchema()
    {
        var initializer = new SqliteDatabaseInitializer(_options);
        await initializer.InitializeAsync();

        await using var connection = new SqliteConnection($"Data Source={_databasePath}");
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM sqlite_master
            WHERE type = 'table'
              AND name NOT LIKE 'sqlite_%';
            """;

        var tableCount = Convert.ToInt64(await command.ExecuteScalarAsync());

        Assert.Equal(0L, tableCount);
    }

    [Fact]
    public async Task InitializeAsync_RejectsForeignSqliteDatabase()
    {
        await using (var connection = new SqliteConnection($"Data Source={_databasePath}"))
        {
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA application_id = 12345;";
            await command.ExecuteNonQueryAsync();
        }

        var initializer = new SqliteDatabaseInitializer(_options);

        var exception = await Assert.ThrowsAsync<SubTrackPersistenceException>(
            () => initializer.InitializeAsync());

        Assert.Equal(PersistenceFailureKind.ForeignDatabase, exception.Kind);

        await using var verificationConnection = new SqliteConnection($"Data Source={_databasePath}");
        await verificationConnection.OpenAsync();

        using var verificationCommand = verificationConnection.CreateCommand();
        verificationCommand.CommandText = "PRAGMA application_id;";
        var applicationId = Convert.ToInt64(await verificationCommand.ExecuteScalarAsync());

        Assert.Equal(12_345L, applicationId);
    }

    [Fact]
    public async Task InitializeAsync_RejectsNonSqliteFile()
    {
        await File.WriteAllTextAsync(_databasePath, "esto no es una base de datos SQLite");

        var initializer = new SqliteDatabaseInitializer(_options);

        var exception = await Assert.ThrowsAsync<SubTrackPersistenceException>(
            () => initializer.InitializeAsync());

        Assert.Equal(PersistenceFailureKind.NotADatabase, exception.Kind);
    }

    [Fact]
    public async Task OpenAsync_EnforcesForeignKeysAndBusyTimeout()
    {
        var initializer = new SqliteDatabaseInitializer(_options);
        await initializer.InitializeAsync();

        var factory = new SqliteConnectionFactory(_options.Normalize());

        await using var connection = await factory.OpenAsync(CancellationToken.None);

        using (var createCommand = connection.CreateCommand())
        {
            createCommand.CommandText = """
                CREATE TABLE parent (
                    id TEXT NOT NULL PRIMARY KEY
                );

                CREATE TABLE child (
                    id TEXT NOT NULL PRIMARY KEY,
                    parent_id TEXT NOT NULL,
                    CONSTRAINT fk_child_parent
                        FOREIGN KEY (parent_id) REFERENCES parent(id)
                );
                """;

            await createCommand.ExecuteNonQueryAsync();
        }

        using (var busyTimeoutCommand = connection.CreateCommand())
        {
            busyTimeoutCommand.CommandText = "PRAGMA busy_timeout;";
            var busyTimeout = Convert.ToInt64(await busyTimeoutCommand.ExecuteScalarAsync());
            Assert.Equal(2_000L, busyTimeout);
        }

        using var insertCommand = connection.CreateCommand();
        insertCommand.CommandText = """
            INSERT INTO child (id, parent_id)
            VALUES ($id, $parentId);
            """;
        insertCommand.Parameters.AddWithValue("$id", Guid.NewGuid().ToString("D"));
        insertCommand.Parameters.AddWithValue("$parentId", Guid.NewGuid().ToString("D"));

        var exception = await Assert.ThrowsAsync<SqliteException>(
            () => insertCommand.ExecuteNonQueryAsync());

        Assert.Equal(787, exception.SqliteExtendedErrorCode);
    }

    [Fact]
    public void CreateDefault_UsesPrivateLocalApplicationDataPath()
    {
        var options = SubTrackDatabaseOptions.CreateDefault();

        Assert.True(Path.IsPathFullyQualified(options.DatabasePath));
        Assert.EndsWith(
            Path.Combine("SubTrack", "data", "subtrack.db"),
            options.DatabasePath,
            StringComparison.OrdinalIgnoreCase);
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
