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
    public async Task MigrateAsync_FromHistoricalV1AndFromNewDatabase_ProducesEquivalentSchema()
    {
        var historicalV1 = OfficialMigrations.All[0];
        var v1Migrator = new SqliteDatabaseMigrator(
            _options,
            new StaticSqliteMigrationProvider([historicalV1]));
        await v1Migrator.MigrateAsync();
        await SeedHistoricalV1DataAsync(_databasePath);

        var upgradeResult = await new SqliteDatabaseMigrator(_options).MigrateAsync();
        Assert.Equal(OfficialMigrations.GuidValidationVersion, upgradeResult.FinalVersion);

        var newDatabasePath = Path.Combine(_tempDirectory, "subtrack-new-install.db");
        var newOptions = new SubTrackDatabaseOptions
        {
            DatabasePath = newDatabasePath,
            BusyTimeoutMilliseconds = 2_000
        };
        var newInstallResult = await new SqliteDatabaseMigrator(newOptions).MigrateAsync();
        Assert.Equal(OfficialMigrations.GuidValidationVersion, newInstallResult.FinalVersion);

        Assert.Equal(
            await ReadLogicalSchemaAsync(_databasePath),
            await ReadLogicalSchemaAsync(newDatabasePath));
        Assert.Equal(0, await ReadForeignKeyViolationsAsync(_databasePath));
        Assert.Equal(0, await ReadForeignKeyViolationsAsync(newDatabasePath));

        await using var connection = new SqliteConnection($"Data Source={_databasePath}");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT s.nombre, c.estado, p.estado, p.monto_centavos
            FROM pagos p
            JOIN cobros_programados c ON c.id = p.cobro_id
            JOIN suscripciones s ON s.id = c.suscripcion_id
            WHERE p.id = 'eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee';
            """;
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal("Historical subscription", reader.GetString(0));
        Assert.Equal("PENDIENTE", reader.GetString(1));
        Assert.Equal("VIGENTE", reader.GetString(2));
        Assert.Equal(1500, reader.GetInt64(3));
        Assert.False(await reader.ReadAsync());
        await reader.DisposeAsync();

        command.CommandText = "PRAGMA user_version;";
        Assert.Equal(OfficialMigrations.GuidValidationVersion, Convert.ToInt32(await command.ExecuteScalarAsync()));

        command.CommandText = $"""
            SELECT checksum FROM {SqliteDatabaseMigrator.MigrationsTableName} WHERE version = 1;
            """;
        Assert.Equal(
            "a79092c05bda71f7cb92807110c1293c43ad0555ab1fe87d6e1820e16697298f",
            (string?)await command.ExecuteScalarAsync());
    }

    [Fact]
    public async Task MigrateAsync_V2RejectsInvalidHistoricalGuidWithoutChangingDataOrHistory()
    {
        var v1 = OfficialMigrations.All[0];
        await new SqliteDatabaseMigrator(
            _options,
            new StaticSqliteMigrationProvider([v1])).MigrateAsync();

        await using (var connection = new SqliteConnection($"Data Source={_databasePath}"))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO categorias_gasto
                    (id, nombre, activa, creado_en, actualizado_en)
                VALUES
                    ('zzzzzzzz-zzzz-zzzz-zzzz-zzzzzzzzzzzz',
                     'legacy-invalid-guid', 1, '2026-10-25T00:00:00.000Z',
                     '2026-10-25T00:00:00.000Z');
                """;
            await command.ExecuteNonQueryAsync();
        }

        var exception = await Assert.ThrowsAsync<SubTrackPersistenceException>(
            () => new SqliteDatabaseMigrator(_options).MigrateAsync());

        Assert.Equal(PersistenceFailureKind.MigrationFailed, exception.Kind);
        Assert.Contains("invalid GUID", exception.Message, StringComparison.OrdinalIgnoreCase);

        await using var verificationConnection = new SqliteConnection($"Data Source={_databasePath}");
        await verificationConnection.OpenAsync();
        await using var verificationCommand = verificationConnection.CreateCommand();
        verificationCommand.CommandText = """
            SELECT id FROM categorias_gasto WHERE nombre = 'legacy-invalid-guid';
            """;
        Assert.Equal(
            "zzzzzzzz-zzzz-zzzz-zzzz-zzzzzzzzzzzz",
            (string?)await verificationCommand.ExecuteScalarAsync());

        verificationCommand.CommandText = $"""
            SELECT COUNT(*) FROM {SqliteDatabaseMigrator.MigrationsTableName};
            """;
        Assert.Equal(1, Convert.ToInt32(await verificationCommand.ExecuteScalarAsync()));
        verificationCommand.CommandText = "PRAGMA user_version;";
        Assert.Equal(1, Convert.ToInt32(await verificationCommand.ExecuteScalarAsync()));
        verificationCommand.CommandText = """
            SELECT COUNT(*) FROM sqlite_master
            WHERE type = 'trigger' AND name = 'trg_v2_guid_categorias_gasto_insert';
            """;
        Assert.Equal(0, Convert.ToInt32(await verificationCommand.ExecuteScalarAsync()));
    }

    [Fact]
    public async Task MigrateAsync_V2RejectsLegacyVigentePaymentWithMismatchedAmountAndRollsBack()
    {
        await PrepareHistoricalV1DatabaseAsync();

        await using (var connection = new SqliteConnection($"Data Source={_databasePath}"))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = """
                UPDATE pagos
                SET estado = 'ANULADO',
                    monto_centavos = 1000,
                    anulado_en = '2026-10-28T00:00:00.000Z',
                    motivo_anulacion = 'legacy mismatch'
                WHERE id = 'eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee';

                UPDATE pagos
                SET estado = 'VIGENTE',
                    anulado_en = NULL,
                    motivo_anulacion = NULL
                WHERE id = 'eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee';
                """;
            await command.ExecuteNonQueryAsync();
        }

        await AssertLegacyPaymentRejectedAndV1PreservedAsync(
            "existing VIGENTE payments",
            "cccccccc-cccc-4ccc-8ccc-cccccccccccc",
            "VIGENTE",
            1000);
    }

    [Fact]
    public async Task MigrateAsync_V2RejectsLegacyVigentePaymentWithoutChargeAndRollsBack()
    {
        await PrepareHistoricalV1DatabaseAsync();

        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = _databasePath,
            ForeignKeys = false
        }.ToString();
        await using (var connection = new SqliteConnection(connectionString))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = """
                UPDATE pagos
                SET cobro_id = 'ffffffff-ffff-4fff-8fff-ffffffffffff'
                WHERE id = 'eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee';
                """;
            await command.ExecuteNonQueryAsync();
        }

        await AssertLegacyPaymentRejectedAndV1PreservedAsync(
            "existing VIGENTE payments",
            "ffffffff-ffff-4fff-8fff-ffffffffffff",
            "VIGENTE",
            1500);
    }

    [Fact]
    public async Task MigrateAsync_V2AcceptsLegacyAnnulledPaymentWithMismatchedAmount()
    {
        await PrepareHistoricalV1DatabaseAsync();

        await using (var connection = new SqliteConnection($"Data Source={_databasePath}"))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = """
                UPDATE pagos
                SET estado = 'ANULADO',
                    monto_centavos = 1000,
                    anulado_en = '2026-10-28T00:00:00.000Z',
                    motivo_anulacion = 'legacy mismatch'
                WHERE id = 'eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee';
                """;
            await command.ExecuteNonQueryAsync();
        }

        var result = await new SqliteDatabaseMigrator(_options).MigrateAsync();
        Assert.Equal(OfficialMigrations.GuidValidationVersion, result.FinalVersion);

        await using var verificationConnection = new SqliteConnection($"Data Source={_databasePath}");
        await verificationConnection.OpenAsync();
        await using var verificationCommand = verificationConnection.CreateCommand();
        verificationCommand.CommandText = """
            SELECT estado, monto_centavos FROM pagos
            WHERE id = 'eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee';
            """;
        await using var reader = await verificationCommand.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal("ANULADO", reader.GetString(0));
        Assert.Equal(1000, reader.GetInt64(1));
        Assert.False(await reader.ReadAsync());
        await reader.DisposeAsync();

        verificationCommand.CommandText = "PRAGMA foreign_key_check;";
        await using var foreignKeyReader = await verificationCommand.ExecuteReaderAsync();
        Assert.False(await foreignKeyReader.ReadAsync());
    }

    [Fact]
    public async Task MigrateAsync_WhenV2Fails_RollsBackItsSchemaDataHistoryAndUserVersion()
    {
        var v1 = OfficialMigrations.All[0];
        await new SqliteDatabaseMigrator(
            _options,
            new StaticSqliteMigrationProvider([v1])).MigrateAsync();
        await SeedHistoricalV1DataAsync(_databasePath);

        var officialV2 = OfficialMigrations.All[1];
        var failingV2 = new SqliteMigration(
            officialV2.Version,
            officialV2.Name,
            officialV2.SqlContent + Environment.NewLine + "SINTAXIS_SQL_INVALIDA_TOTAL;");
        var exception = await Assert.ThrowsAsync<SubTrackPersistenceException>(
            () => new SqliteDatabaseMigrator(
                _options,
                new StaticSqliteMigrationProvider([v1, failingV2])).MigrateAsync());

        Assert.Equal(PersistenceFailureKind.MigrationFailed, exception.Kind);

        await using var connection = new SqliteConnection($"Data Source={_databasePath}");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*) FROM sqlite_master
            WHERE type = 'trigger' AND name = 'trg_v2_guid_pagos_insert';
            """;
        Assert.Equal(0, Convert.ToInt32(await command.ExecuteScalarAsync()));

        command.CommandText = $"""
            SELECT COUNT(*) FROM {SqliteDatabaseMigrator.MigrationsTableName};
            """;
        Assert.Equal(1, Convert.ToInt32(await command.ExecuteScalarAsync()));
        command.CommandText = $"""
            SELECT checksum FROM {SqliteDatabaseMigrator.MigrationsTableName} WHERE version = 1;
            """;
        Assert.Equal(v1.CalculateChecksum(), await command.ExecuteScalarAsync());
        command.CommandText = "PRAGMA user_version;";
        Assert.Equal(1, Convert.ToInt32(await command.ExecuteScalarAsync()));
        command.CommandText = """
            SELECT COUNT(*) FROM pagos
            WHERE id = 'eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee'
              AND monto_centavos = 1500;
            """;
        Assert.Equal(1, Convert.ToInt32(await command.ExecuteScalarAsync()));
        command.CommandText = "PRAGMA foreign_key_check;";
        await using var reader = await command.ExecuteReaderAsync();
        Assert.False(await reader.ReadAsync());
    }

    private static async Task SeedHistoricalV1DataAsync(string databasePath)
    {
        await using var connection = new SqliteConnection($"Data Source={databasePath}");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO suscripciones
                (id, nombre, operacion_id, monto_centavos, tipo_importe, frecuencia,
                 fecha_inicio, proximo_cobro, activa, desactivada_en, creado_en, actualizado_en)
            VALUES
                ('aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa', 'Historical subscription',
                 'aaaaaaaa-0000-4000-8000-000000000001', 1500, 'FIJO', 'MENSUAL',
                 '2026-10-01', '2026-11-01', 1, NULL,
                 '2026-10-25T00:00:00.000Z', '2026-10-25T00:00:00.000Z');

            INSERT INTO suscripcion_versiones
                (id, suscripcion_id, numero_version, vigente_desde, monto_centavos,
                 tipo_importe, frecuencia, dia_ancla, creado_en)
            VALUES
                ('bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb',
                 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa', 1, '2026-10-01', 1500,
                 'FIJO', 'MENSUAL', 1, '2026-10-25T00:00:00.000Z');

            INSERT INTO cobros_programados
                (id, suscripcion_id, fecha_programada, monto_centavos, estado, creado_en, actualizado_en)
            VALUES
                ('cccccccc-cccc-4ccc-8ccc-cccccccccccc',
                 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa', '2026-11-01', 1500,
                 'PENDIENTE', '2026-10-25T00:00:00.000Z', '2026-10-25T00:00:00.000Z');

            INSERT INTO pagos
                (id, cobro_id, operacion_id, fecha_pago, monto_centavos, estado,
                 numero_comprobante, anulado_en, motivo_anulacion, creado_en, actualizado_en)
            VALUES
                ('eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee',
                 'cccccccc-cccc-4ccc-8ccc-cccccccccccc',
                 'eeeeeeee-0000-4000-8000-000000000001', '2026-10-27', 1500,
                 'VIGENTE', 'CMP-2026-000001', NULL, NULL,
                 '2026-10-25T00:00:00.000Z', '2026-10-25T00:00:00.000Z');
            """;
        await command.ExecuteNonQueryAsync();
    }

    private async Task PrepareHistoricalV1DatabaseAsync()
    {
        var v1 = OfficialMigrations.All[0];
        await new SqliteDatabaseMigrator(
            _options,
            new StaticSqliteMigrationProvider([v1])).MigrateAsync();
        await SeedHistoricalV1DataAsync(_databasePath);
    }

    private async Task AssertLegacyPaymentRejectedAndV1PreservedAsync(
        string expectedError,
        string expectedChargeId,
        string expectedState,
        long expectedAmount)
    {
        var exception = await Assert.ThrowsAsync<SubTrackPersistenceException>(
            () => new SqliteDatabaseMigrator(_options).MigrateAsync());
        Assert.Equal(PersistenceFailureKind.MigrationFailed, exception.Kind);
        Assert.Contains(expectedError, exception.Message, StringComparison.OrdinalIgnoreCase);

        await using var connection = new SqliteConnection($"Data Source={_databasePath}");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT cobro_id, estado, monto_centavos, anulado_en, motivo_anulacion
            FROM pagos
            WHERE id = 'eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee';
            """;
        await using (var reader = await command.ExecuteReaderAsync())
        {
            Assert.True(await reader.ReadAsync());
            Assert.Equal(expectedChargeId, reader.GetString(0));
            Assert.Equal(expectedState, reader.GetString(1));
            Assert.Equal(expectedAmount, reader.GetInt64(2));
            if (expectedState == "ANULADO")
            {
                Assert.False(reader.IsDBNull(3));
                Assert.Equal("legacy mismatch", reader.GetString(4));
            }
            else
            {
                Assert.True(reader.IsDBNull(3));
                Assert.True(reader.IsDBNull(4));
            }
        }

        command.CommandText = $"""
            SELECT COUNT(*) FROM {SqliteDatabaseMigrator.MigrationsTableName};
            """;
        Assert.Equal(1, Convert.ToInt32(await command.ExecuteScalarAsync()));
        command.CommandText = $"""
            SELECT checksum FROM {SqliteDatabaseMigrator.MigrationsTableName} WHERE version = 1;
            """;
        Assert.Equal(
            "a79092c05bda71f7cb92807110c1293c43ad0555ab1fe87d6e1820e16697298f",
            (string?)await command.ExecuteScalarAsync());
        command.CommandText = "PRAGMA user_version;";
        Assert.Equal(1, Convert.ToInt32(await command.ExecuteScalarAsync()));

        command.CommandText = """
            SELECT sql FROM sqlite_master
            WHERE type = 'trigger' AND name = 'trg_pagos_importe_completo';
            """;
        var insertTriggerSql = (string?)await command.ExecuteScalarAsync();
        Assert.NotNull(insertTriggerSql);
        Assert.Contains("BEFORE INSERT ON pagos", insertTriggerSql, StringComparison.OrdinalIgnoreCase);

        command.CommandText = """
            SELECT sql FROM sqlite_master
            WHERE type = 'trigger' AND name = 'trg_pagos_importe_completo_update';
            """;
        var updateTriggerSql = (string?)await command.ExecuteScalarAsync();
        Assert.NotNull(updateTriggerSql);
        Assert.Contains(
            "BEFORE UPDATE OF monto_centavos, cobro_id ON pagos",
            updateTriggerSql,
            StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("UPDATE OF estado", updateTriggerSql, StringComparison.OrdinalIgnoreCase);

        command.CommandText = """
            SELECT COUNT(*) FROM sqlite_master
            WHERE type = 'trigger' AND name = 'trg_v2_guid_pagos_insert';
            """;
        Assert.Equal(0, Convert.ToInt32(await command.ExecuteScalarAsync()));
    }

    private static async Task<IReadOnlyList<string>> ReadLogicalSchemaAsync(string databasePath)
    {
        await using var connection = new SqliteConnection($"Data Source={databasePath}");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT type || '|' || name || '|' || tbl_name || '|' || COALESCE(sql, '')
            FROM sqlite_master
            WHERE substr(name, 1, 7) <> 'sqlite_'
            ORDER BY type, name;
            """;

        var schema = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            schema.Add(reader.GetString(0));
        }

        return schema;
    }

    private static async Task<int> ReadForeignKeyViolationsAsync(string databasePath)
    {
        await using var connection = new SqliteConnection($"Data Source={databasePath}");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_key_check;";

        var count = 0;
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            count++;
        }

        return count;
    }

    [Fact]
    public async Task MigrateAsync_WithDefaultProvider_AppliesOfficialMigrationsAndPreservesHistoricalV1Checksum()
    {
        var migrator = new SqliteDatabaseMigrator(_options);

        var result = await migrator.MigrateAsync();

        Assert.True(result.Success);
        Assert.False(result.IsUpToDate);
        Assert.Equal(0, result.InitialVersion);
        Assert.Equal(OfficialMigrations.GuidValidationVersion, result.FinalVersion);
        Assert.Equal(2, result.AppliedMigrations.Count);
        Assert.Equal(
            OfficialMigrations.InitialSchemaVersion,
            result.AppliedMigrations[0].Version);
        Assert.Equal(
            "a79092c05bda71f7cb92807110c1293c43ad0555ab1fe87d6e1820e16697298f",
            OfficialMigrations.All[0].CalculateChecksum());
        Assert.Equal(
            OfficialMigrations.GuidValidationVersion,
            result.AppliedMigrations[1].Version);

        var currentVersion = await migrator.GetCurrentVersionAsync();
        Assert.Equal(OfficialMigrations.GuidValidationVersion, currentVersion);

        var history = await migrator.GetAppliedMigrationsAsync();
        Assert.Equal(2, history.Count);
        Assert.Equal(OfficialMigrations.InitialSchemaName, history[0].Name);
        Assert.Equal(
            "a79092c05bda71f7cb92807110c1293c43ad0555ab1fe87d6e1820e16697298f",
            history[0].Checksum);
        Assert.Equal(OfficialMigrations.GuidValidationName, history[1].Name);
    }

    [Fact]
    public async Task MigrateAsync_OnNewDatabase_AssignsIdentityAndAppliesMigrations()
    {
        var migrations = new[]
        {
            new SqliteMigration(1, "v1", "CREATE TABLE migrated_data (id INTEGER PRIMARY KEY);")
        };
        var migrator = new SqliteDatabaseMigrator(
            _options,
            new StaticSqliteMigrationProvider(migrations));

        var result = await migrator.MigrateAsync();

        Assert.Equal(1, result.FinalVersion);
        Assert.Equal(SqliteDatabaseMigrator.ApplicationId, await ReadApplicationIdAsync());
        Assert.True(await TableExistsAsync("migrated_data"));
        Assert.True(await TableExistsAsync(SqliteDatabaseMigrator.MigrationsTableName));
    }

    [Fact]
    public async Task MigrateAsync_OnValidSubTrackDatabase_AllowsMigrations()
    {
        await using (var connection = new SqliteConnection($"Data Source={_databasePath}"))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = $"""
                PRAGMA application_id = {SqliteDatabaseMigrator.ApplicationId};
                CREATE TABLE existing_data (value TEXT NOT NULL);
                INSERT INTO existing_data (value) VALUES ('preserve-me');
                """;
            await command.ExecuteNonQueryAsync();
        }

        var migrations = new[]
        {
            new SqliteMigration(1, "v1", "CREATE TABLE migrated_data (id INTEGER PRIMARY KEY);")
        };
        var migrator = new SqliteDatabaseMigrator(
            _options,
            new StaticSqliteMigrationProvider(migrations));

        var result = await migrator.MigrateAsync();

        Assert.Equal(1, result.FinalVersion);
        Assert.True(await TableExistsAsync("migrated_data"));
        Assert.True(await TableExistsAsync(SqliteDatabaseMigrator.MigrationsTableName));

        await using var verificationConnection = new SqliteConnection($"Data Source={_databasePath}");
        await verificationConnection.OpenAsync();
        await using var verificationCommand = verificationConnection.CreateCommand();
        verificationCommand.CommandText = "SELECT value FROM existing_data;";
        Assert.Equal("preserve-me", await verificationCommand.ExecuteScalarAsync());
    }

    [Fact]
    public async Task MigrateAsync_WithDefaultProvider_IsIdempotentOnSecondRun()
    {
        var migrator = new SqliteDatabaseMigrator(_options);

        await migrator.MigrateAsync();
        var secondResult = await migrator.MigrateAsync();

        Assert.True(secondResult.Success);
        Assert.True(secondResult.IsUpToDate);
        Assert.Equal(OfficialMigrations.GuidValidationVersion, secondResult.InitialVersion);
        Assert.Equal(OfficialMigrations.GuidValidationVersion, secondResult.FinalVersion);
        Assert.Empty(secondResult.AppliedMigrations);
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
    public async Task MigrateAsync_WhenOnlyUserVersionIsFuture_ThrowsFutureVersion()
    {
        await using (var connection = new SqliteConnection($"Data Source={_databasePath}"))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = $"""
                PRAGMA application_id = {SqliteDatabaseMigrator.ApplicationId};
                PRAGMA user_version = 3;
                """;
            await command.ExecuteNonQueryAsync();
        }

        var migrations = new[]
        {
            new SqliteMigration(1, "v1", "CREATE TABLE v1 (id INTEGER PRIMARY KEY);"),
            new SqliteMigration(2, "v2", "CREATE TABLE v2 (id INTEGER PRIMARY KEY);")
        };
        var migrator = new SqliteDatabaseMigrator(
            _options,
            new StaticSqliteMigrationProvider(migrations));

        var exception = await Assert.ThrowsAsync<SubTrackPersistenceException>(
            () => migrator.MigrateAsync());

        Assert.Equal(PersistenceFailureKind.FutureVersion, exception.Kind);

        await using var verificationConnection = new SqliteConnection($"Data Source={_databasePath}");
        await verificationConnection.OpenAsync();
        await using var verificationCommand = verificationConnection.CreateCommand();
        verificationCommand.CommandText = "PRAGMA user_version;";
        Assert.Equal(3, Convert.ToInt32(await verificationCommand.ExecuteScalarAsync()));

        verificationCommand.CommandText = """
            SELECT COUNT(*)
            FROM esquema_migraciones;
            """;
        Assert.Equal(0, Convert.ToInt32(await verificationCommand.ExecuteScalarAsync()));
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

        checkCmd.CommandText = $"""
            SELECT COUNT(*)
            FROM {SqliteDatabaseMigrator.MigrationsTableName}
            WHERE version = 1;
            """;
        Assert.Equal(1, Convert.ToInt32(await checkCmd.ExecuteScalarAsync()));

        checkCmd.CommandText = $"""
            SELECT COUNT(*)
            FROM {SqliteDatabaseMigrator.MigrationsTableName}
            WHERE version = 2;
            """;
        Assert.Equal(0, Convert.ToInt32(await checkCmd.ExecuteScalarAsync()));

        checkCmd.CommandText = "PRAGMA user_version;";
        Assert.Equal(1, Convert.ToInt32(await checkCmd.ExecuteScalarAsync()));
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
    public async Task MigrateAsync_RejectsUnidentifiedDatabaseWithSqliteXTableWithoutChangingIt()
    {
        await using (var connection = new SqliteConnection($"Data Source={_databasePath}"))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = """
                CREATE TABLE sqliteX_data (id INTEGER PRIMARY KEY, value TEXT NOT NULL);
                INSERT INTO sqliteX_data (id, value) VALUES (1, 'preserve-me');
                """;
            await command.ExecuteNonQueryAsync();
        }

        await AssertUnidentifiedDatabaseRejectedWithoutAdoptionAsync();

        await using var verificationConnection = new SqliteConnection($"Data Source={_databasePath}");
        await verificationConnection.OpenAsync();
        await using var verificationCommand = verificationConnection.CreateCommand();
        verificationCommand.CommandText = "SELECT value FROM sqliteX_data WHERE id = 1;";
        Assert.Equal("preserve-me", await verificationCommand.ExecuteScalarAsync());
    }

    [Fact]
    public async Task MigrateAsync_RejectsUnidentifiedDatabaseWithOnlyUserViewWithoutChangingIt()
    {
        await using (var connection = new SqliteConnection($"Data Source={_databasePath}"))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "CREATE VIEW foreign_view AS SELECT 'preserve-me' AS value;";
            await command.ExecuteNonQueryAsync();
        }

        await AssertUnidentifiedDatabaseRejectedWithoutAdoptionAsync();

        await using var verificationConnection = new SqliteConnection($"Data Source={_databasePath}");
        await verificationConnection.OpenAsync();
        await using var verificationCommand = verificationConnection.CreateCommand();
        verificationCommand.CommandText = "SELECT value FROM foreign_view;";
        Assert.Equal("preserve-me", await verificationCommand.ExecuteScalarAsync());
    }

    [Fact]
    public async Task MigrateAsync_RejectsUnidentifiedDatabaseWithOrdinaryTableAndPreservesData()
    {
        await using (var connection = new SqliteConnection($"Data Source={_databasePath}"))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = """
                CREATE TABLE foreign_records (id INTEGER PRIMARY KEY, value TEXT NOT NULL);
                INSERT INTO foreign_records (id, value) VALUES (1, 'preserve-me');
                """;
            await command.ExecuteNonQueryAsync();
        }

        await AssertUnidentifiedDatabaseRejectedWithoutAdoptionAsync();

        await using var verificationConnection = new SqliteConnection($"Data Source={_databasePath}");
        await verificationConnection.OpenAsync();
        await using var verificationCommand = verificationConnection.CreateCommand();
        verificationCommand.CommandText = "SELECT value FROM foreign_records WHERE id = 1;";
        Assert.Equal("preserve-me", await verificationCommand.ExecuteScalarAsync());
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

    private async Task AssertUnidentifiedDatabaseRejectedWithoutAdoptionAsync()
    {
        var migrator = new SqliteDatabaseMigrator(_options);
        var exception = await Assert.ThrowsAsync<SubTrackPersistenceException>(
            () => migrator.MigrateAsync());

        Assert.Equal(PersistenceFailureKind.ForeignDatabase, exception.Kind);
        Assert.Equal(0L, await ReadApplicationIdAsync());
        Assert.False(await TableExistsAsync(SqliteDatabaseMigrator.MigrationsTableName));
    }

    private async Task<long> ReadApplicationIdAsync()
    {
        await using var connection = new SqliteConnection($"Data Source={_databasePath}");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA application_id;";
        return Convert.ToInt64(await command.ExecuteScalarAsync());
    }

    private async Task<bool> TableExistsAsync(string tableName)
    {
        await using var connection = new SqliteConnection($"Data Source={_databasePath}");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT EXISTS (
                SELECT 1 FROM sqlite_master
                WHERE type = 'table' AND name = $name);
            """;
        command.Parameters.AddWithValue("$name", tableName);
        return Convert.ToInt64(await command.ExecuteScalarAsync()) != 0;
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
