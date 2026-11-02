using Microsoft.Data.Sqlite;
using SubTrack.Infrastructure.Persistence;

namespace SubTrack.Tests.Infrastructure;

public sealed class SqliteBackupServiceTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly string _databasePath;
    private readonly string _backupDirectory;
    private readonly SubTrackDatabaseOptions _dbOptions;
    private readonly SubTrackBackupOptions _backupOptions;

    public SqliteBackupServiceTests()
    {
        _tempDirectory = Path.Combine(
            Path.GetTempPath(),
            "SubTrack.Tests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(_tempDirectory);

        _databasePath = Path.Combine(_tempDirectory, "data", "subtrack-backup-test.db");
        _backupDirectory = Path.Combine(_tempDirectory, "backups");
        Directory.CreateDirectory(_backupDirectory);

        _dbOptions = new SubTrackDatabaseOptions
        {
            DatabasePath = _databasePath,
            BusyTimeoutMilliseconds = 3_000
        };

        _backupOptions = new SubTrackBackupOptions
        {
            BackupDirectoryPath = _backupDirectory,
            BackupFilePrefix = "test_backup",
            DefaultRetentionCount = 10
        };
    }

    private async Task InitializeDatabaseWithSampleDataAsync()
    {
        var initializer = new SqliteDatabaseInitializer(_dbOptions);
        await initializer.InitializeAsync();

        var migrations = new[]
        {
            new SqliteMigration(1, "crear_tabla_muestras", @"
                CREATE TABLE muestras (
                    id INTEGER PRIMARY KEY,
                    valor TEXT NOT NULL
                );
                INSERT INTO muestras (valor) VALUES ('muestra_inicial_1'), ('muestra_inicial_2');")
        };

        var provider = new StaticSqliteMigrationProvider(migrations);
        var migrator = new SqliteDatabaseMigrator(_dbOptions, provider);
        await migrator.MigrateAsync();
    }

    [Fact]
    public async Task CreateBackupAsync_WithActiveWalMode_ProducesConsistentValidBackup()
    {
        await InitializeDatabaseWithSampleDataAsync();

        var service = new SqliteBackupService(_dbOptions, _backupOptions);

        var result = await service.CreateBackupAsync();

        Assert.NotNull(result.BackupPath);
        Assert.True(File.Exists(result.BackupPath));
        Assert.True(result.FileSizeBytes > 0);
        Assert.Equal(SqliteBackupService.ExpectedApplicationId, result.ApplicationId);
        Assert.Equal(1, result.UserVersion);
        Assert.Equal("ok", result.IntegrityCheckResult, ignoreCase: true);

        // Verificar el contenido del archivo de respaldo directamente
        await using var connection = new SqliteConnection($"Data Source={result.BackupPath};Mode=ReadOnly");
        await connection.OpenAsync();

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM muestras;";
        var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task VerifyBackupAsync_ValidatesCorrectly_AndRejectsInvalidOrCorruptedFiles()
    {
        await InitializeDatabaseWithSampleDataAsync();
        var service = new SqliteBackupService(_dbOptions, _backupOptions);

        var backupResult = await service.CreateBackupAsync();

        // 1. Respaldo legítimo
        var validVerification = await service.VerifyBackupAsync(backupResult.BackupPath);
        Assert.True(validVerification.IsValid);
        Assert.Equal("ok", validVerification.IntegrityStatus, ignoreCase: true);
        Assert.Null(validVerification.FailureReason);
        Assert.Equal(1, validVerification.MigrationsCount);

        // 2. Archivo inexistente
        var missingVerification = await service.VerifyBackupAsync(
            Path.Combine(_backupDirectory, "inexistente.db"));
        Assert.False(missingVerification.IsValid);
        Assert.Equal("file_not_found", missingVerification.IntegrityStatus);

        // 3. Archivo vacío (0 bytes)
        var emptyFilePath = Path.Combine(_backupDirectory, "vacio.db");
        await File.WriteAllBytesAsync(emptyFilePath, Array.Empty<byte>());
        var emptyVerification = await service.VerifyBackupAsync(emptyFilePath);
        Assert.False(emptyVerification.IsValid);
        Assert.Equal("empty_file", emptyVerification.IntegrityStatus);

        // 4. Archivo corrupto (basura binaria)
        var corruptFilePath = Path.Combine(_backupDirectory, "corrupto.db");
        await File.WriteAllTextAsync(corruptFilePath, "ESTO_NO_ES_SQLITE_CABECERA_BASURA_BINARIA_1234567890");
        var corruptVerification = await service.VerifyBackupAsync(corruptFilePath);
        Assert.False(corruptVerification.IsValid);

        // 5. Base de datos SQLite ajena (sin application_id de SubTrack)
        var foreignDbPath = Path.Combine(_backupDirectory, "ajena.db");
        await using (var conn = new SqliteConnection($"Data Source={foreignDbPath}"))
        {
            await conn.OpenAsync();
            await using var foreignCmd = conn.CreateCommand();
            foreignCmd.CommandText = "PRAGMA application_id = 999999; CREATE TABLE otra (x INT);";
            await foreignCmd.ExecuteNonQueryAsync();
        }

        var foreignVerification = await service.VerifyBackupAsync(foreignDbPath);
        Assert.False(foreignVerification.IsValid);
        Assert.Equal("foreign_database", foreignVerification.IntegrityStatus);
    }

    [Fact]
    public async Task RestoreBackupAsync_ReplacesActiveDatabaseSafely()
    {
        await InitializeDatabaseWithSampleDataAsync();
        var service = new SqliteBackupService(_dbOptions, _backupOptions);

        // Tomar respaldo del estado inicial (2 muestras)
        var backup = await service.CreateBackupAsync();

        // Mutar la base activa agregando 3 registros más (total 5)
        await using (var conn = new SqliteConnection($"Data Source={_databasePath}"))
        {
            await conn.OpenAsync();
            await using var insertCmd = conn.CreateCommand();
            insertCmd.CommandText = @"
                INSERT INTO muestras (valor) VALUES ('m3'), ('m4'), ('m5');";
            await insertCmd.ExecuteNonQueryAsync();
        }

        // Verificar que la base activa tiene 5 registros
        await using (var checkConn = new SqliteConnection($"Data Source={_databasePath}"))
        {
            await checkConn.OpenAsync();
            await using var countCmd = checkConn.CreateCommand();
            countCmd.CommandText = "SELECT COUNT(*) FROM muestras;";
            var mutatedCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
            Assert.Equal(5, mutatedCount);
        }

        // Restaurar el respaldo original
        var restoreResult = await service.RestoreBackupAsync(backup.BackupPath, keepSafetyBackup: true);

        Assert.Equal(Path.GetFullPath(_databasePath), restoreResult.RestoredDatabasePath);
        Assert.Equal(backup.BackupPath, restoreResult.SourceBackupPath);
        Assert.NotNull(restoreResult.SafetyBackupPath);
        Assert.True(File.Exists(restoreResult.SafetyBackupPath));

        // Verificar que la base restaurada volvió a tener exactamente 2 registros
        await using (var restoredConn = new SqliteConnection($"Data Source={_databasePath}"))
        {
            await restoredConn.OpenAsync();
            await using var countCmd = restoredConn.CreateCommand();
            countCmd.CommandText = "SELECT COUNT(*) FROM muestras;";
            var restoredCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
            Assert.Equal(2, restoredCount);

            // Verificar que los pragmas (WAL) siguen activos
            await using var journalCmd = restoredConn.CreateCommand();
            journalCmd.CommandText = "PRAGMA journal_mode;";
            var journal = (string?)await journalCmd.ExecuteScalarAsync();
            Assert.Equal("wal", journal, ignoreCase: true);
        }
    }

    [Fact]
    public async Task RestoreBackupAsync_WhenCandidateBackupIsCorrupted_AbortsAndPreservesActiveDatabase()
    {
        await InitializeDatabaseWithSampleDataAsync();
        var service = new SqliteBackupService(_dbOptions, _backupOptions);

        // Archivo corrupto candidato
        var corruptBackupPath = Path.Combine(_backupDirectory, "backup_falso_corrupto.db");
        await File.WriteAllTextAsync(corruptBackupPath, "CONTENIDO_INVALIDO_TOTAL");

        // Intentar restaurar
        var exception = await Assert.ThrowsAsync<SubTrackPersistenceException>(
            () => service.RestoreBackupAsync(corruptBackupPath));

        Assert.Equal(PersistenceFailureKind.InvalidBackup, exception.Kind);

        // Comprobar que la base activa original sigue intacta y funcional
        await using var conn = new SqliteConnection($"Data Source={_databasePath}");
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM muestras;";
        var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task ListAndPruneBackups_ManageRetentionCorrectly()
    {
        await InitializeDatabaseWithSampleDataAsync();
        var service = new SqliteBackupService(_dbOptions, _backupOptions);

        // Generar 3 respaldos espaciados
        var backup1 = await service.CreateBackupAsync();
        await Task.Delay(15);
        var backup2 = await service.CreateBackupAsync();
        await Task.Delay(15);
        var backup3 = await service.CreateBackupAsync();

        var list = await service.ListBackupsAsync();
        Assert.True(list.Count >= 3);
        // Debe estar ordenado de más reciente a más antiguo
        Assert.True(list[0].CreatedAt >= list[1].CreatedAt);

        // Podar conservando únicamente los 2 más recientes
        var prunedCount = await service.PruneBackupsAsync(keepCount: 2);
        Assert.True(prunedCount >= 1);

        var listAfterPrune = await service.ListBackupsAsync();
        Assert.Equal(2, listAfterPrune.Count);
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
