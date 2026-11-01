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
    public async Task CreateBackupAsync_IncludesCommittedDataStillInWal()
    {
        await InitializeDatabaseWithSampleDataAsync();
        await using var activeConnection = new SqliteConnection(
            $"Data Source={_databasePath};Pooling=False");
        await activeConnection.OpenAsync();

        await using (var setupCommand = activeConnection.CreateCommand())
        {
            setupCommand.CommandText = "PRAGMA wal_autocheckpoint = 0;";
            await setupCommand.ExecuteNonQueryAsync();
        }

        await using (var insertCommand = activeConnection.CreateCommand())
        {
            insertCommand.CommandText = "INSERT INTO muestras (valor) VALUES ('en-wal');";
            await insertCommand.ExecuteNonQueryAsync();
        }

        Assert.True(File.Exists(_databasePath + "-wal"));

        var service = new SqliteBackupService(_dbOptions, _backupOptions);
        var backup = await service.CreateBackupAsync();

        await using var backupConnection = new SqliteConnection(
            $"Data Source={backup.BackupPath};Mode=ReadOnly");
        await backupConnection.OpenAsync();
        await using var countCommand = backupConnection.CreateCommand();
        countCommand.CommandText = "SELECT COUNT(*) FROM muestras;";

        Assert.Equal(3, Convert.ToInt32(await countCommand.ExecuteScalarAsync()));
    }

    [Fact]
    public async Task CreateBackupAsync_AtomicallyReplacesAnExistingBackup()
    {
        await InitializeDatabaseWithSampleDataAsync();
        var service = new SqliteBackupService(_dbOptions, _backupOptions);
        var originalBackup = await service.CreateBackupAsync();

        await using (var connection = new SqliteConnection($"Data Source={_databasePath}"))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO muestras (valor) VALUES ('nuevo');";
            await command.ExecuteNonQueryAsync();
        }

        var replacement = await service.CreateBackupAsync(originalBackup.BackupPath);

        Assert.Equal(originalBackup.BackupPath, replacement.BackupPath);
        var verification = await service.VerifyBackupAsync(replacement.BackupPath);
        Assert.True(verification.IsValid);

        await using var backupConnection = new SqliteConnection(
            $"Data Source={replacement.BackupPath};Mode=ReadOnly");
        await backupConnection.OpenAsync();
        await using var countCommand = backupConnection.CreateCommand();
        countCommand.CommandText = "SELECT COUNT(*) FROM muestras;";

        Assert.Equal(3, Convert.ToInt32(await countCommand.ExecuteScalarAsync()));
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
    public async Task VerifyAndRestoreBackupAsync_WithForeignKeyViolation_RejectsBackupAndPreservesActiveDatabase()
    {
        await InitializeDatabaseWithSampleDataAsync();
        await AddSampleAsync("active-original");

        var invalidBackupPath = Path.Combine(_backupDirectory, "orphaned-backup.db");
        await using (var connection = new SqliteConnection(
                         new SqliteConnectionStringBuilder
                         {
                             DataSource = invalidBackupPath,
                             Mode = SqliteOpenMode.ReadWriteCreate,
                             Pooling = false
                         }.ToString()))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = $"""
                PRAGMA foreign_keys = OFF;
                PRAGMA application_id = {SqliteBackupService.ExpectedApplicationId};
                PRAGMA user_version = 1;
                CREATE TABLE parents (id INTEGER PRIMARY KEY);
                CREATE TABLE children (
                    parent_id INTEGER NOT NULL REFERENCES parents(id)
                );
                INSERT INTO children (parent_id) VALUES (42);
                """;
            await command.ExecuteNonQueryAsync();
        }

        var service = new SqliteBackupService(_dbOptions, _backupOptions);
        var verification = await service.VerifyBackupAsync(invalidBackupPath);

        Assert.False(verification.IsValid);
        Assert.Equal("foreign_key_violation", verification.IntegrityStatus);
        Assert.Contains("children", verification.FailureReason);

        var exception = await Assert.ThrowsAsync<SubTrackPersistenceException>(
            () => service.RestoreBackupAsync(invalidBackupPath));

        Assert.Equal(PersistenceFailureKind.InvalidBackup, exception.Kind);
        Assert.Equal(3, await ReadSampleCountAsync());
        Assert.Contains("active-original", await ReadSampleValuesAsync());
        Assert.Empty(Directory.GetFiles(
            Path.GetDirectoryName(_databasePath)!,
            "*.pre_restore_*.bak"));
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

        var safetyVerification = await service.VerifyBackupAsync(restoreResult.SafetyBackupPath);
        Assert.True(safetyVerification.IsValid);

        await using (var safetyConnection = new SqliteConnection(
                         $"Data Source={restoreResult.SafetyBackupPath};Mode=ReadOnly"))
        {
            await safetyConnection.OpenAsync();
            await using var safetyCountCommand = safetyConnection.CreateCommand();
            safetyCountCommand.CommandText = "SELECT COUNT(*) FROM muestras;";
            Assert.Equal(5, Convert.ToInt32(await safetyCountCommand.ExecuteScalarAsync()));
        }

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
    public async Task RestoreBackupAsync_WhenSafetySnapshotCreationFails_PreservesActiveDatabase()
    {
        await InitializeDatabaseWithSampleDataAsync();
        var standardService = new SqliteBackupService(_dbOptions, _backupOptions);
        var candidateBackup = await standardService.CreateBackupAsync();
        await AddSampleAsync("active-original");

        string? stagingPath = null;
        var service = CreateServiceWithFaultInjector((point, path) =>
        {
            if (point == SqliteBackupFaultPoint.AfterSafetySnapshotCreation)
            {
                stagingPath = path;
                Assert.True(File.Exists(path));
                Assert.True(new FileInfo(path).Length > 0);
                throw new IOException("Simulated failure after VACUUM INTO created the snapshot.");
            }
        });

        await Assert.ThrowsAsync<SubTrackPersistenceException>(
            () => service.RestoreBackupAsync(candidateBackup.BackupPath));

        Assert.NotNull(stagingPath);
        Assert.False(File.Exists(stagingPath));
        Assert.Empty(Directory.GetFiles(
            Path.GetDirectoryName(_databasePath)!,
            "*.pre_restore_*.bak"));

        await using (var connection = new SqliteConnection($"Data Source={_databasePath}"))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT COUNT(*)
                FROM sqlite_master
                WHERE type = 'table' AND name IN ('muestras', 'esquema_migraciones');
                """;
            Assert.Equal(2, Convert.ToInt32(await command.ExecuteScalarAsync()));
        }

        Assert.Equal(3, await ReadSampleCountAsync());
        Assert.Contains("active-original", await ReadSampleValuesAsync());
    }

    [Fact]
    public async Task RestoreBackupAsync_WhenSafetySnapshotValidationFails_DoesNotUsePartialFile()
    {
        await InitializeDatabaseWithSampleDataAsync();
        var standardService = new SqliteBackupService(_dbOptions, _backupOptions);
        var candidateBackup = await standardService.CreateBackupAsync();
        await AddSampleAsync("active-original");

        var service = CreateServiceWithFaultInjector((point, path) =>
        {
            if (point == SqliteBackupFaultPoint.BeforeSafetySnapshotValidation)
            {
                File.WriteAllText(path, "partial-invalid-snapshot");
            }
        });

        var exception = await Assert.ThrowsAsync<SubTrackPersistenceException>(
            () => service.RestoreBackupAsync(candidateBackup.BackupPath));

        Assert.Equal(PersistenceFailureKind.RestoreFailed, exception.Kind);
        Assert.Equal(3, await ReadSampleCountAsync());
        Assert.Contains("active-original", await ReadSampleValuesAsync());
        Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(_databasePath)!, "*.pre_restore_*.bak"));
    }

    [Fact]
    public async Task RestoreBackupAsync_WhenFailureOccursAfterReplacement_RecoversOriginalDatabase()
    {
        await InitializeDatabaseWithSampleDataAsync();
        var standardService = new SqliteBackupService(_dbOptions, _backupOptions);
        var candidateBackup = await standardService.CreateBackupAsync();
        await AddSampleAsync("active-original");

        var service = CreateServiceWithFaultInjector((point, _) =>
        {
            if (point == SqliteBackupFaultPoint.AfterRestoredDatabaseReplacement)
            {
                throw new IOException("Simulated failure after replacing active database.");
            }
        });

        await Assert.ThrowsAsync<SubTrackPersistenceException>(
            () => service.RestoreBackupAsync(candidateBackup.BackupPath));

        Assert.Equal(3, await ReadSampleCountAsync());
        Assert.Contains("active-original", await ReadSampleValuesAsync());

        var safetyPath = Assert.Single(
            Directory.GetFiles(Path.GetDirectoryName(_databasePath)!, "*.pre_restore_*.bak"));
        var verification = await standardService.VerifyBackupAsync(safetyPath);
        Assert.True(verification.IsValid);

        await using var safetyConnection = new SqliteConnection(
            $"Data Source={safetyPath};Mode=ReadOnly");
        await safetyConnection.OpenAsync();
        await using var safetyCommand = safetyConnection.CreateCommand();
        safetyCommand.CommandText = "SELECT COUNT(*) FROM muestras;";
        Assert.Equal(3, Convert.ToInt32(await safetyCommand.ExecuteScalarAsync()));
    }

    [Fact]
    public async Task RestoreBackupAsync_WhenRecoveryFails_KeepsValidatedSafetyBackupForManualRecovery()
    {
        await InitializeDatabaseWithSampleDataAsync();
        var standardService = new SqliteBackupService(_dbOptions, _backupOptions);
        var candidateBackup = await standardService.CreateBackupAsync();
        await AddSampleAsync("active-original");

        var service = CreateServiceWithFaultInjector((point, _) =>
        {
            if (point == SqliteBackupFaultPoint.AfterRestoredDatabaseReplacement
                || point == SqliteBackupFaultPoint.BeforeRecoveryDatabaseInitialization)
            {
                throw new IOException($"Simulated failure at {point}.");
            }
        });

        var exception = await Assert.ThrowsAsync<SubTrackPersistenceException>(
            () => service.RestoreBackupAsync(candidateBackup.BackupPath));

        Assert.Equal(PersistenceFailureKind.RestoreFailed, exception.Kind);

        await using (var activeConnection = new SqliteConnection($"Data Source={_databasePath}"))
        {
            await activeConnection.OpenAsync();
            await using var activeCommand = activeConnection.CreateCommand();
            activeCommand.CommandText = """
                SELECT COUNT(*)
                FROM sqlite_master
                WHERE type = 'table' AND name IN ('muestras', 'esquema_migraciones');
                """;
            Assert.Equal(2, Convert.ToInt32(await activeCommand.ExecuteScalarAsync()));

            activeCommand.CommandText = "SELECT COUNT(*) FROM muestras;";
            Assert.Equal(3, Convert.ToInt32(await activeCommand.ExecuteScalarAsync()));

            activeCommand.CommandText =
                "SELECT COUNT(*) FROM muestras WHERE valor = 'active-original';";
            Assert.Equal(1, Convert.ToInt32(await activeCommand.ExecuteScalarAsync()));
        }

        var safetyPath = Assert.Single(
            Directory.GetFiles(Path.GetDirectoryName(_databasePath)!, "*.pre_restore_*.bak"));
        var verification = await standardService.VerifyBackupAsync(safetyPath);
        Assert.True(verification.IsValid);

        await using var safetyConnection = new SqliteConnection(
            $"Data Source={safetyPath};Mode=ReadOnly");
        await safetyConnection.OpenAsync();
        await using var safetyCommand = safetyConnection.CreateCommand();
        safetyCommand.CommandText = "SELECT COUNT(*) FROM muestras;";
        Assert.Equal(3, Convert.ToInt32(await safetyCommand.ExecuteScalarAsync()));
        safetyCommand.CommandText = "SELECT COUNT(*) FROM muestras WHERE valor = 'active-original';";
        Assert.Equal(1, Convert.ToInt32(await safetyCommand.ExecuteScalarAsync()));
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

        var unrelatedDatabasePath = Path.Combine(_backupDirectory, "other_application.db");
        await using (var unrelatedConnection = new SqliteConnection(
                         $"Data Source={unrelatedDatabasePath}"))
        {
            await unrelatedConnection.OpenAsync();
            await using var command = unrelatedConnection.CreateCommand();
            command.CommandText = "CREATE TABLE unrelated (id INTEGER PRIMARY KEY);";
            await command.ExecuteNonQueryAsync();
        }

        var temporaryFilePath = Path.Combine(
            _backupDirectory,
            "test_backup_20261101_120000_000.db.staging.tmp");
        await File.WriteAllTextAsync(temporaryFilePath, "temporary");

        var malformedBackupPath = Path.Combine(_backupDirectory, "test_backup_not-a-timestamp.db");
        await File.WriteAllTextAsync(malformedBackupPath, "not an official backup");

        var list = await service.ListBackupsAsync();
        Assert.Equal(3, list.Count);
        // Debe estar ordenado de más reciente a más antiguo
        Assert.True(list[0].CreatedAt >= list[1].CreatedAt);

        // Podar conservando únicamente los 2 más recientes
        var prunedCount = await service.PruneBackupsAsync(keepCount: 2);
        Assert.Equal(1, prunedCount);

        var listAfterPrune = await service.ListBackupsAsync();
        Assert.Equal(2, listAfterPrune.Count);
        Assert.True(File.Exists(unrelatedDatabasePath));
        Assert.True(File.Exists(temporaryFilePath));
        Assert.True(File.Exists(malformedBackupPath));
        Assert.False(File.Exists(backup1.BackupPath));
        Assert.True(File.Exists(backup2.BackupPath));
        Assert.True(File.Exists(backup3.BackupPath));
    }

    [Fact]
    public async Task PruneBackupsAsync_DoesNotDeleteActiveDatabaseMatchingBackupNameFormat()
    {
        var activeDatabasePath = Path.Combine(
            _backupDirectory,
            "test_backup_20000101_000000_000.db");
        var databaseOptions = new SubTrackDatabaseOptions
        {
            DatabasePath = activeDatabasePath,
            BusyTimeoutMilliseconds = 3_000
        };
        await new SqliteDatabaseInitializer(databaseOptions).InitializeAsync();

        var service = new SqliteBackupService(databaseOptions, _backupOptions);
        var createdBackup = await service.CreateBackupAsync();

        var prunedCount = await service.PruneBackupsAsync(keepCount: 1);

        Assert.Equal(0, prunedCount);
        Assert.True(File.Exists(activeDatabasePath));
        Assert.True(File.Exists(createdBackup.BackupPath));
        Assert.Single(await service.ListBackupsAsync());
        Assert.Equal(createdBackup.BackupPath, (await service.ListBackupsAsync()).Single().FilePath);
    }

    [Fact]
    public async Task PruneBackupsAsync_WhenBackupCannotBeDeleted_ReportsPersistenceFailure()
    {
        await InitializeDatabaseWithSampleDataAsync();
        var service = new SqliteBackupService(_dbOptions, _backupOptions);

        await service.CreateBackupAsync();
        await Task.Delay(15);
        await service.CreateBackupAsync();

        var oldestBackup = (await service.ListBackupsAsync()).Last();
        using var lockedBackup = new FileStream(
            oldestBackup.FilePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.None);

        var exception = await Assert.ThrowsAsync<SubTrackPersistenceException>(
            () => service.PruneBackupsAsync(keepCount: 1));

        Assert.Equal(PersistenceFailureKind.IoError, exception.Kind);
        Assert.Equal(oldestBackup.FilePath, exception.DatabasePath);
    }

    private SqliteBackupService CreateServiceWithFaultInjector(
        Action<SqliteBackupFaultPoint, string> faultInjector) =>
        new(_dbOptions, _backupOptions, faultInjector);

    private async Task AddSampleAsync(string value)
    {
        await using var connection = new SqliteConnection($"Data Source={_databasePath}");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO muestras (valor) VALUES ($value);";
        command.Parameters.AddWithValue("$value", value);
        await command.ExecuteNonQueryAsync();
    }

    private async Task<int> ReadSampleCountAsync()
    {
        await using var connection = new SqliteConnection($"Data Source={_databasePath}");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM muestras;";
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    private async Task<IReadOnlyList<string>> ReadSampleValuesAsync()
    {
        await using var connection = new SqliteConnection($"Data Source={_databasePath}");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT valor FROM muestras ORDER BY id;";
        var values = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            values.Add(reader.GetString(0));
        }

        return values;
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
