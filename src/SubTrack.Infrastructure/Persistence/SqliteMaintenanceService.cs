using System.Globalization;
using Microsoft.Data.Sqlite;

namespace SubTrack.Infrastructure.Persistence;

/// <summary>
/// Operaciones de diagnóstico y mantenimiento no destructivo para la base SQLite de SubTrack.
/// No crea bases nuevas, no repara datos, no elimina filas y no ejecuta VACUUM completo.
/// </summary>
public sealed class SqliteMaintenanceService
{
    private const string IntegrityCheckSql = "PRAGMA integrity_check;";
    private const string ForeignKeyCheckSql = "PRAGMA foreign_key_check;";

    private readonly SubTrackDatabaseOptions _options;
    private readonly SqliteConnectionFactory _connectionFactory;

    public SqliteMaintenanceService(SubTrackDatabaseOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options.Normalize();
        _connectionFactory = new SqliteConnectionFactory(_options);
    }

    /// <summary>
    /// Obtiene métricas técnicas de la base sin modificar su contenido.
    /// </summary>
    public Task<SqliteDatabaseDiagnostics> GetDiagnosticsAsync(
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            (connection, ct) => ReadDiagnosticsAsync(connection, _options.DatabasePath, ct),
            cancellationToken);
    }

    /// <summary>
    /// Ejecuta <c>PRAGMA integrity_check</c> y <c>PRAGMA foreign_key_check</c>.
    /// Devuelve el detalle de anomalías en lugar de reparar o eliminar datos.
    /// </summary>
    public Task<SqliteIntegrityReport> CheckIntegrityAsync(
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            async (connection, ct) =>
            {
                var integrityMessages = await ReadIntegrityMessagesAsync(connection, ct)
                    .ConfigureAwait(false);

                var foreignKeyViolations = await ReadForeignKeyViolationsAsync(connection, ct)
                    .ConfigureAwait(false);

                var diagnostics = await ReadDiagnosticsAsync(connection, _options.DatabasePath, ct)
                    .ConfigureAwait(false);

                return new SqliteIntegrityReport(
                    _options.DatabasePath,
                    integrityMessages,
                    foreignKeyViolations,
                    diagnostics,
                    DateTimeOffset.UtcNow);
            },
            cancellationToken);
    }

    /// <summary>
    /// Ejecuta <c>PRAGMA optimize</c> para actualizar estadísticas internas del planificador.
    /// No reconstruye la base ni elimina información.
    /// </summary>
    public Task<SqliteMaintenanceResult> OptimizeAsync(
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            async (connection, ct) =>
            {
                await using var command = connection.CreateCommand();
                command.CommandText = "PRAGMA optimize;";
                await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);

                return new SqliteMaintenanceResult(
                    "optimize",
                    Success: true,
                    "PRAGMA optimize ejecutado sin acciones destructivas.");
            },
            cancellationToken);
    }

    /// <summary>
    /// Ejecuta un checkpoint WAL explícito y devuelve el estado informado por SQLite.
    /// </summary>
    public Task<SqliteWalCheckpointResult> CheckpointWalAsync(
        SqliteWalCheckpointMode mode = SqliteWalCheckpointMode.Passive,
        CancellationToken cancellationToken = default)
    {
        var modeSql = mode switch
        {
            SqliteWalCheckpointMode.Passive => "PASSIVE",
            SqliteWalCheckpointMode.Full => "FULL",
            SqliteWalCheckpointMode.Restart => "RESTART",
            SqliteWalCheckpointMode.Truncate => "TRUNCATE",
            _ => throw new ArgumentOutOfRangeException(
                nameof(mode),
                mode,
                "Modo de checkpoint WAL no soportado.")
        };

        return ExecuteAsync(
            async (connection, ct) =>
            {
                var journalMode = await ReadStringAsync(
                    connection,
                    "PRAGMA journal_mode;",
                    ct).ConfigureAwait(false);

                if (!string.Equals(journalMode, "wal", StringComparison.OrdinalIgnoreCase))
                {
                    return new SqliteWalCheckpointResult(
                        mode,
                        Executed: false,
                        Busy: 0,
                        LogPages: 0,
                        CheckpointedPages: 0,
                        "La base de datos no está en modo WAL; no se ejecutó checkpoint.");
                }

                await using var command = connection.CreateCommand();
                command.CommandText = $"PRAGMA wal_checkpoint({modeSql});";

                await using var reader = await command.ExecuteReaderAsync(ct)
                    .ConfigureAwait(false);

                if (!await reader.ReadAsync(ct).ConfigureAwait(false))
                {
                    return new SqliteWalCheckpointResult(
                        mode,
                        Executed: false,
                        Busy: -1,
                        LogPages: -1,
                        CheckpointedPages: -1,
                        "SQLite no devolvió resultado para el checkpoint.");
                }

                var busy = reader.GetInt32(0);
                var logPages = reader.GetInt32(1);
                var checkpointedPages = reader.GetInt32(2);

                return new SqliteWalCheckpointResult(
                    mode,
                    Executed: true,
                    busy,
                    logPages,
                    checkpointedPages,
                    busy == 0
                        ? "Checkpoint WAL completado."
                        : "Checkpoint WAL parcial: hay lectores o escritores activos.");
            },
            cancellationToken);
    }

    private async Task<T> ExecuteAsync<T>(
        Func<SqliteConnection, CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        EnsureDatabaseExists();

        try
        {
            await using var connection = await _connectionFactory
                .OpenAsync(cancellationToken)
                .ConfigureAwait(false);

            await EnsureSubTrackIdentityAsync(connection, cancellationToken).ConfigureAwait(false);

            return await operation(connection, cancellationToken).ConfigureAwait(false);
        }
        catch (SqliteException exception)
        {
            throw SqliteErrorMapper.Map(exception, _options.DatabasePath);
        }
        catch (UnauthorizedAccessException exception)
        {
            throw new SubTrackPersistenceException(
                "No hay permisos para acceder a la base de datos.",
                PersistenceFailureKind.CannotOpen,
                _options.DatabasePath,
                innerException: exception);
        }
        catch (IOException exception)
        {
            throw new SubTrackPersistenceException(
                "No se pudo acceder al archivo de la base de datos.",
                PersistenceFailureKind.IoError,
                _options.DatabasePath,
                innerException: exception);
        }
    }

    private static async Task EnsureSubTrackIdentityAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        var applicationId = await ReadInt64Async(
            connection,
            "PRAGMA application_id;",
            cancellationToken).ConfigureAwait(false);

        if (applicationId != SqliteDatabaseInitializer.ApplicationId)
        {
            throw new SubTrackPersistenceException(
                $"El archivo SQLite no pertenece a SubTrack (application_id={applicationId}).",
                PersistenceFailureKind.ForeignDatabase);
        }
    }

    private static async Task<SqliteDatabaseDiagnostics> ReadDiagnosticsAsync(
        SqliteConnection connection,
        string databasePath,
        CancellationToken cancellationToken)
    {
        var applicationId = await ReadInt64Async(
            connection,
            "PRAGMA application_id;",
            cancellationToken).ConfigureAwait(false);

        var userVersion = (int)await ReadInt64Async(
            connection,
            "PRAGMA user_version;",
            cancellationToken).ConfigureAwait(false);

        var journalMode = await ReadStringAsync(
            connection,
            "PRAGMA journal_mode;",
            cancellationToken).ConfigureAwait(false);

        var pageSize = await ReadInt64Async(
            connection,
            "PRAGMA page_size;",
            cancellationToken).ConfigureAwait(false);

        var pageCount = await ReadInt64Async(
            connection,
            "PRAGMA page_count;",
            cancellationToken).ConfigureAwait(false);

        var freelistPageCount = await ReadInt64Async(
            connection,
            "PRAGMA freelist_count;",
            cancellationToken).ConfigureAwait(false);

        var hasMigrationsTable = await TableExistsAsync(
            connection,
            SqliteDatabaseMigrator.MigrationsTableName,
            cancellationToken).ConfigureAwait(false);

        var migrationsCount = 0;
        if (hasMigrationsTable)
        {
            migrationsCount = (int)await ReadInt64Async(
                connection,
                $"SELECT COUNT(*) FROM {SqliteDatabaseMigrator.MigrationsTableName};",
                cancellationToken).ConfigureAwait(false);
        }

        return new SqliteDatabaseDiagnostics(
            databasePath,
            applicationId,
            userVersion,
            hasMigrationsTable,
            migrationsCount,
            journalMode,
            pageSize,
            pageCount,
            freelistPageCount,
            GetFileSize(databasePath),
            GetFileSize(databasePath + "-wal"),
            GetFileSize(databasePath + "-shm"));
    }

    private static async Task<IReadOnlyList<string>> ReadIntegrityMessagesAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = IntegrityCheckSql;

        var messages = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            if (!reader.IsDBNull(0))
            {
                messages.Add(reader.GetString(0));
            }
        }

        return messages;
    }

    private static async Task<IReadOnlyList<SqliteForeignKeyViolation>> ReadForeignKeyViolationsAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = ForeignKeyCheckSql;

        var violations = new List<SqliteForeignKeyViolation>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var table = reader.GetString(0);
            long? rowId = reader.IsDBNull(1) ? null : reader.GetInt64(1);
            var parentTable = reader.GetString(2);
            var foreignKeyIndex = reader.GetInt32(3);

            violations.Add(new SqliteForeignKeyViolation(
                table,
                rowId,
                parentTable,
                foreignKeyIndex));
        }

        return violations;
    }

    private static async Task<bool> TableExistsAsync(
        SqliteConnection connection,
        string tableName,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = @name LIMIT 1;";
        command.Parameters.AddWithValue("@name", tableName);

        var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return result is not null && result is not DBNull;
    }

    private static async Task<long> ReadInt64Async(
        SqliteConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        var value = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return value is null or DBNull
            ? 0L
            : Convert.ToInt64(value, CultureInfo.InvariantCulture);
    }

    private static async Task<string> ReadStringAsync(
        SqliteConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        var value = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return value is null or DBNull
            ? string.Empty
            : Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private static long GetFileSize(string path)
    {
        try
        {
            return File.Exists(path) ? new FileInfo(path).Length : 0L;
        }
        catch (IOException)
        {
            return 0L;
        }
        catch (UnauthorizedAccessException)
        {
            return 0L;
        }
    }

    private void EnsureDatabaseExists()
    {
        if (!File.Exists(_options.DatabasePath))
        {
            throw new SubTrackPersistenceException(
                "La base de datos no existe. La inspección no crea archivos nuevos.",
                PersistenceFailureKind.CannotOpen,
                _options.DatabasePath);
        }
    }
}
