using System.Data;
using System.Diagnostics;
using System.Globalization;
using Microsoft.Data.Sqlite;

namespace SubTrack.Infrastructure.Persistence;

/// <summary>
/// Motor de migraciones SQLite versionadas y transaccionales para SubTrack.
/// Gestiona la evolución del esquema garantizando idempotencia, integridad de checksums,
/// detección de versiones futuras y reversión atómica ante fallos.
/// </summary>
public sealed class SqliteDatabaseMigrator
{
    public const string MigrationsTableName = "esquema_migraciones";
    public const long ApplicationId = 1_398_096_468; // 'SUBT'

    private readonly SubTrackDatabaseOptions _options;
    private readonly SqliteConnectionFactory _connectionFactory;
    private readonly ISqliteMigrationProvider _migrationProvider;

    public SqliteDatabaseMigrator(SubTrackDatabaseOptions options)
        : this(options, new DefaultSqliteMigrationProvider())
    {
    }

    public SqliteDatabaseMigrator(
        SubTrackDatabaseOptions options,
        ISqliteMigrationProvider migrationProvider)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(migrationProvider);

        _options = options.Normalize();
        _connectionFactory = new SqliteConnectionFactory(_options);
        _migrationProvider = migrationProvider;
    }

    /// <summary>
    /// Ejecuta todas las migraciones pendientes de forma secuencial y transaccional.
    /// Valida previamente la integridad de las migraciones ya aplicadas.
    /// </summary>
    public async Task<MigrationResult> MigrateAsync(CancellationToken cancellationToken = default)
    {
        EnsureDataDirectory();

        await using var connection = await _connectionFactory
            .OpenAsync(cancellationToken)
            .ConfigureAwait(false);

        await EnsureDatabaseIdentityAsync(connection, cancellationToken).ConfigureAwait(false);
        await EnsureMigrationsTableAsync(connection, cancellationToken).ConfigureAwait(false);

        var appliedMigrations = await ReadAppliedMigrationsInternalAsync(connection, cancellationToken)
            .ConfigureAwait(false);

        var availableMigrations = GetValidatedAvailableMigrations();

        var maxAppliedVersion = appliedMigrations.Count > 0
            ? appliedMigrations.Max(m => m.Version)
            : 0;

        var maxAvailableVersion = availableMigrations.Count > 0
            ? availableMigrations.Max(m => m.Version)
            : 0;

        var userVersion = await ReadUserVersionAsync(connection, cancellationToken).ConfigureAwait(false);

        // 1. Detección de versiones futuras
        if (maxAppliedVersion > maxAvailableVersion || userVersion > maxAvailableVersion)
        {
            var detectedVersion = Math.Max(maxAppliedVersion, userVersion);
            throw new SubTrackPersistenceException(
                $"La base de datos contiene la versión {detectedVersion}, superior a la versión máxima " +
                $"soportada ({maxAvailableVersion}) por esta versión de la aplicación.",
                PersistenceFailureKind.FutureVersion,
                _options.DatabasePath);
        }

        // 2. Validación de checksums e integridad histórica de migraciones aplicadas
        var appliedByVersion = appliedMigrations.ToDictionary(m => m.Version);
        var availableByVersion = availableMigrations.ToDictionary(m => m.Version);

        for (var v = 1; v <= maxAppliedVersion; v++)
        {
            if (!appliedByVersion.ContainsKey(v))
            {
                throw new SubTrackPersistenceException(
                    $"Inconsistencia en el historial de migraciones: la versión {v} no figura como aplicada " +
                    $"en la base de datos pero se registraron versiones posteriores (hasta {maxAppliedVersion}).",
                    PersistenceFailureKind.MigrationFailed,
                    _options.DatabasePath);
            }
        }

        foreach (var (version, applied) in appliedByVersion)
        {
            if (!availableByVersion.TryGetValue(version, out var available))
            {
                throw new SubTrackPersistenceException(
                    $"Inconsistencia en el historial de migraciones: la versión {version} ('{applied.Name}') " +
                    "figura como aplicada en la base de datos pero no existe en el catálogo de la aplicación.",
                    PersistenceFailureKind.MigrationFailed,
                    _options.DatabasePath);
            }

            var expectedChecksum = available.CalculateChecksum();
            if (!string.Equals(applied.Checksum, expectedChecksum, StringComparison.OrdinalIgnoreCase))
            {
                throw new SubTrackPersistenceException(
                    $"Inconsistencia de integridad: el checksum registrado para la migración versión {version} " +
                    $"('{applied.Name}') no coincide con el script actual de la aplicación.\n" +
                    $"Registrado: {applied.Checksum}\nActual:     {expectedChecksum}",
                    PersistenceFailureKind.ChecksumMismatch,
                    _options.DatabasePath);
            }
        }

        // 3. Determinar migraciones pendientes
        var pendingMigrations = availableMigrations
            .Where(m => m.Version > maxAppliedVersion)
            .OrderBy(m => m.Version)
            .ToList();

        // 4. Si está al día, sincronizar el header SQLite y retornar (idempotente)
        if (pendingMigrations.Count == 0)
        {
            await SynchronizeUserVersionAsync(
                connection,
                maxAppliedVersion,
                userVersion,
                cancellationToken).ConfigureAwait(false);

            return new MigrationResult(
                Success: true,
                InitialVersion: maxAppliedVersion,
                FinalVersion: maxAppliedVersion,
                AppliedMigrations: Array.Empty<MigrationExecutionResult>());
        }

        // 5. Validar que la secuencia pendiente no contenga huecos ni discontinuidades
        for (var i = 0; i < pendingMigrations.Count; i++)
        {
            var expectedVersion = maxAppliedVersion + i + 1;
            if (pendingMigrations[i].Version != expectedVersion)
            {
                throw new SubTrackPersistenceException(
                    $"Secuencia de migraciones discontinua: se esperaba la versión {expectedVersion}, " +
                    $"pero la siguiente pendiente es la versión {pendingMigrations[i].Version}.",
                    PersistenceFailureKind.MigrationFailed,
                    _options.DatabasePath);
            }
        }

        // 6. Aplicar cada migración pendiente de forma transaccional y atómica
        var executionResults = new List<MigrationExecutionResult>();
        var currentVersion = maxAppliedVersion;

        foreach (var migration in pendingMigrations)
        {
            var executionResult = await ApplySingleMigrationAsync(
                connection,
                migration,
                cancellationToken).ConfigureAwait(false);

            executionResults.Add(executionResult);
            currentVersion = migration.Version;
        }

        return new MigrationResult(
            Success: true,
            InitialVersion: maxAppliedVersion,
            FinalVersion: currentVersion,
            AppliedMigrations: executionResults);
    }

    /// <summary>
    /// Consulta el historial completo de migraciones aplicadas en la base de datos.
    /// </summary>
    public async Task<IReadOnlyList<AppliedMigrationRecord>> GetAppliedMigrationsAsync(
        CancellationToken cancellationToken = default)
    {
        EnsureDataDirectory();

        await using var connection = await _connectionFactory
            .OpenAsync(cancellationToken)
            .ConfigureAwait(false);

        await EnsureDatabaseIdentityAsync(connection, cancellationToken).ConfigureAwait(false);
        await EnsureMigrationsTableAsync(connection, cancellationToken).ConfigureAwait(false);

        return await ReadAppliedMigrationsInternalAsync(connection, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Obtiene la versión actual registrada en la base de datos.
    /// </summary>
    public async Task<int> GetCurrentVersionAsync(CancellationToken cancellationToken = default)
    {
        EnsureDataDirectory();

        await using var connection = await _connectionFactory
            .OpenAsync(cancellationToken)
            .ConfigureAwait(false);

        await EnsureDatabaseIdentityAsync(connection, cancellationToken).ConfigureAwait(false);
        await EnsureMigrationsTableAsync(connection, cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COALESCE(MAX(version), 0) FROM {MigrationsTableName};";

        var scalar = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return Convert.ToInt32(scalar, CultureInfo.InvariantCulture);
    }

    private async Task<MigrationExecutionResult> ApplySingleMigrationAsync(
        SqliteConnection connection,
        SqliteMigration migration,
        CancellationToken cancellationToken)
    {
        await using var transaction = (SqliteTransaction)await connection
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // 1. Ejecutar el script DDL/DML de la migración
            await using (var migrationCmd = connection.CreateCommand())
            {
                migrationCmd.Transaction = transaction;
                migrationCmd.CommandText = migration.SqlContent;
                migrationCmd.CommandTimeout = Math.Max(30, _options.BusyTimeoutMilliseconds / 1000);
                await migrationCmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            stopwatch.Stop();

            // 2. Registrar en la tabla de migraciones
            var checksum = migration.CalculateChecksum();
            var appliedAt = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);

            await using (var insertCmd = connection.CreateCommand())
            {
                insertCmd.Transaction = transaction;
                insertCmd.CommandText = $@"
                    INSERT INTO {MigrationsTableName} (version, nombre, checksum, aplicada_en, duracion_ms)
                    VALUES (@version, @nombre, @checksum, @aplicada_en, @duracion_ms);";

                insertCmd.Parameters.AddWithValue("@version", migration.Version);
                insertCmd.Parameters.AddWithValue("@nombre", migration.Name);
                insertCmd.Parameters.AddWithValue("@checksum", checksum);
                insertCmd.Parameters.AddWithValue("@aplicada_en", appliedAt);
                insertCmd.Parameters.AddWithValue("@duracion_ms", stopwatch.ElapsedMilliseconds);

                await insertCmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            // 3. Sincronizar PRAGMA user_version con la versión de la migración
            await using (var pragmaCmd = connection.CreateCommand())
            {
                pragmaCmd.Transaction = transaction;
                pragmaCmd.CommandText = $"PRAGMA user_version = {migration.Version};";
                await pragmaCmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            // 4. Confirmar transacción
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return new MigrationExecutionResult(
                migration.Version,
                migration.Name,
                checksum,
                stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            try
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // Conservar la excepción original
            }

            if (ex is SubTrackPersistenceException)
            {
                throw;
            }

            if (ex is SqliteException sqlEx)
            {
                throw new SubTrackPersistenceException(
                    $"Error al ejecutar la migración versión {migration.Version} ('{migration.Name}'): {sqlEx.Message}",
                    PersistenceFailureKind.MigrationFailed,
                    _options.DatabasePath,
                    sqlEx.SqliteExtendedErrorCode,
                    sqlEx);
            }

            throw new SubTrackPersistenceException(
                $"Error inesperado al aplicar la migración versión {migration.Version} ('{migration.Name}'): {ex.Message}",
                PersistenceFailureKind.MigrationFailed,
                _options.DatabasePath,
                null,
                ex);
        }
    }

    private static async Task EnsureMigrationsTableAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $@"
            CREATE TABLE IF NOT EXISTS {MigrationsTableName} (
                version      INTEGER NOT NULL PRIMARY KEY CHECK (version > 0),
                nombre       TEXT    NOT NULL CHECK (length(trim(nombre)) > 0),
                checksum     TEXT    NOT NULL CHECK (length(checksum) = 64),
                aplicada_en  TEXT    NOT NULL,
                duracion_ms  INTEGER NOT NULL CHECK (duracion_ms >= 0)
            );";

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<List<AppliedMigrationRecord>> ReadAppliedMigrationsInternalAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $@"
            SELECT version, nombre, checksum, aplicada_en, duracion_ms
            FROM {MigrationsTableName}
            ORDER BY version ASC;";

        var list = new List<AppliedMigrationRecord>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var version = reader.GetInt32(0);
            var nombre = reader.GetString(1);
            var checksum = reader.GetString(2);
            var aplicadaEnStr = reader.GetString(3);
            var duracionMs = reader.GetInt64(4);

            var aplicadaEn = DateTimeOffset.Parse(aplicadaEnStr, CultureInfo.InvariantCulture);

            list.Add(new AppliedMigrationRecord(
                version,
                nombre,
                checksum,
                aplicadaEn,
                duracionMs));
        }

        return list;
    }

    private static async Task<int> ReadUserVersionAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA user_version;";
        var scalar = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return Convert.ToInt32(scalar, CultureInfo.InvariantCulture);
    }

    private static async Task SynchronizeUserVersionAsync(
        SqliteConnection connection,
        int expectedVersion,
        int currentVersion,
        CancellationToken cancellationToken)
    {
        if (currentVersion == expectedVersion)
        {
            return;
        }

        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA user_version = {expectedVersion.ToString(CultureInfo.InvariantCulture)};";
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task EnsureDatabaseIdentityAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA application_id;";
        var scalar = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        var currentAppId = Convert.ToInt64(scalar, CultureInfo.InvariantCulture);

        if (currentAppId == 0)
        {
            await using var setCmd = connection.CreateCommand();
            setCmd.CommandText = $"PRAGMA application_id = {ApplicationId};";
            await setCmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        else if (currentAppId != ApplicationId)
        {
            throw new SubTrackPersistenceException(
                $"El archivo SQLite no pertenece a SubTrack (application_id={currentAppId}).",
                PersistenceFailureKind.ForeignDatabase);
        }
    }

    private IReadOnlyList<SqliteMigration> GetValidatedAvailableMigrations()
    {
        var migrations = _migrationProvider.GetMigrations();
        if (migrations == null || migrations.Count == 0)
        {
            return Array.Empty<SqliteMigration>();
        }

        var ordered = migrations.OrderBy(m => m.Version).ToList();

        // Validar unicidad y secuencia lógica
        for (var i = 0; i < ordered.Count; i++)
        {
            if (ordered[i].Version <= 0)
            {
                throw new SubTrackPersistenceException(
                    $"Versión de migración inválida ({ordered[i].Version}). Debe ser mayor que cero.",
                    PersistenceFailureKind.MigrationFailed,
                    _options.DatabasePath);
            }

            if (i > 0 && ordered[i].Version == ordered[i - 1].Version)
            {
                throw new SubTrackPersistenceException(
                    $"Existe más de una migración con la versión {ordered[i].Version} en el catálogo.",
                    PersistenceFailureKind.MigrationFailed,
                    _options.DatabasePath);
            }
        }

        return ordered;
    }

    private void EnsureDataDirectory()
    {
        var directory = Path.GetDirectoryName(_options.DatabasePath);
        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}
