using System.Globalization;
using Microsoft.Data.Sqlite;

namespace SubTrack.Infrastructure.Persistence;

/// <summary>
/// Inicializa una base SQLite local y verifica las condiciones básicas de
/// integridad, durabilidad y concurrencia. No crea el esquema financiero:
/// eso corresponde a la etapa de migraciones.
/// </summary>
public sealed class SqliteDatabaseInitializer
{
    /// <summary>
    /// Identificador de aplicación SQLite equivalente a 'SUBT'.
    /// Permite rechazar archivos SQLite ajenos antes de modificarlos.
    /// </summary>
    public const long ApplicationId = 1_398_096_468;

    private readonly SubTrackDatabaseOptions _options;
    private readonly SqliteConnectionFactory _connectionFactory;

    public SqliteDatabaseInitializer(SubTrackDatabaseOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options.Normalize();
        _connectionFactory = new SqliteConnectionFactory(_options);
    }

    public async Task<DatabaseInitializationResult> InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            EnsureDataDirectory();

            await using var connection = await _connectionFactory
                .OpenAsync(cancellationToken)
                .ConfigureAwait(false);

            await EnsureDatabaseIdentityAsync(connection, cancellationToken).ConfigureAwait(false);

            var journalMode = await ExecuteScalarStringAsync(
                connection,
                "PRAGMA journal_mode = WAL;",
                cancellationToken).ConfigureAwait(false);

            await ExecuteNonQueryAsync(
                connection,
                "PRAGMA synchronous = FULL;",
                cancellationToken).ConfigureAwait(false);

            await ExecuteNonQueryAsync(
                connection,
                "PRAGMA foreign_keys = ON;",
                cancellationToken).ConfigureAwait(false);

            await ExecuteNonQueryAsync(
                connection,
                $"PRAGMA busy_timeout = {_options.BusyTimeoutMilliseconds};",
                cancellationToken).ConfigureAwait(false);

            var foreignKeys = await ExecuteScalarInt64Async(
                connection,
                "PRAGMA foreign_keys;",
                cancellationToken).ConfigureAwait(false);

            var synchronous = await ExecuteScalarInt64Async(
                connection,
                "PRAGMA synchronous;",
                cancellationToken).ConfigureAwait(false);

            var applicationId = await ExecuteScalarInt64Async(
                connection,
                "PRAGMA application_id;",
                cancellationToken).ConfigureAwait(false);

            var busyTimeout = await ExecuteScalarInt64Async(
                connection,
                "PRAGMA busy_timeout;",
                cancellationToken).ConfigureAwait(false);

            var integrity = await ExecuteScalarStringAsync(
                connection,
                "PRAGMA quick_check;",
                cancellationToken).ConfigureAwait(false);

            if (!string.Equals(journalMode, "wal", StringComparison.OrdinalIgnoreCase))
            {
                throw new SubTrackPersistenceException(
                    "No se pudo activar el modo WAL de SQLite.",
                    PersistenceFailureKind.Unknown,
                    _options.DatabasePath);
            }

            if (foreignKeys != 1)
            {
                throw new SubTrackPersistenceException(
                    "No se pudieron activar las claves foráneas de SQLite.",
                    PersistenceFailureKind.Unknown,
                    _options.DatabasePath);
            }

            if (synchronous != 2)
            {
                throw new SubTrackPersistenceException(
                    "No se pudo configurar SQLite en modo de sincronización FULL.",
                    PersistenceFailureKind.Unknown,
                    _options.DatabasePath);
            }

            if (applicationId != ApplicationId)
            {
                throw new SubTrackPersistenceException(
                    "El archivo no pertenece a SubTrack.",
                    PersistenceFailureKind.ForeignDatabase,
                    _options.DatabasePath);
            }

            if (busyTimeout != _options.BusyTimeoutMilliseconds)
            {
                throw new SubTrackPersistenceException(
                    "No se pudo configurar el tiempo de espera de SQLite.",
                    PersistenceFailureKind.Unknown,
                    _options.DatabasePath);
            }

            if (!string.Equals(integrity, "ok", StringComparison.OrdinalIgnoreCase))
            {
                throw new SubTrackPersistenceException(
                    "La verificación de integridad de la base de datos falló.",
                    PersistenceFailureKind.Corrupt,
                    _options.DatabasePath);
            }

            return new DatabaseInitializationResult(
                _options.DatabasePath,
                journalMode!,
                ForeignKeysEnabled: true,
                SynchronousMode: "FULL",
                ApplicationId: applicationId,
                BusyTimeoutMilliseconds: _options.BusyTimeoutMilliseconds,
                IntegrityCheck: integrity!);
        }
        catch (SqliteException exception)
        {
            throw SqliteErrorMapper.Map(exception, _options.DatabasePath);
        }
        catch (UnauthorizedAccessException exception)
        {
            throw new SubTrackPersistenceException(
                "No hay permisos para crear o acceder a la carpeta de la base de datos.",
                PersistenceFailureKind.CannotOpen,
                _options.DatabasePath,
                innerException: exception);
        }
        catch (IOException exception)
        {
            throw new SubTrackPersistenceException(
                "No se pudo crear o acceder al archivo de la base de datos.",
                PersistenceFailureKind.IoError,
                _options.DatabasePath,
                innerException: exception);
        }
    }

    private async Task EnsureDatabaseIdentityAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        var currentApplicationId = await ExecuteScalarInt64Async(
            connection,
            "PRAGMA application_id;",
            cancellationToken).ConfigureAwait(false);

        if (currentApplicationId == 0)
        {
            await ExecuteNonQueryAsync(
                connection,
                $"PRAGMA application_id = {ApplicationId};",
                cancellationToken).ConfigureAwait(false);

            currentApplicationId = await ExecuteScalarInt64Async(
                connection,
                "PRAGMA application_id;",
                cancellationToken).ConfigureAwait(false);
        }

        if (currentApplicationId != ApplicationId)
        {
            throw new SubTrackPersistenceException(
                "El archivo no pertenece a SubTrack.",
                PersistenceFailureKind.ForeignDatabase,
                _options.DatabasePath);
        }
    }

    private void EnsureDataDirectory()
    {
        var directory = Path.GetDirectoryName(_options.DatabasePath);

        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new SubTrackPersistenceException(
                "La ruta de la base de datos no contiene un directorio válido.",
                PersistenceFailureKind.CannotOpen,
                _options.DatabasePath);
        }

        Directory.CreateDirectory(directory);
    }

    private static async Task ExecuteNonQueryAsync(
        SqliteConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<object?> ExecuteScalarAsync(
        SqliteConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<string?> ExecuteScalarStringAsync(
        SqliteConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        var value = await ExecuteScalarAsync(connection, sql, cancellationToken)
            .ConfigureAwait(false);

        return value is null or DBNull
            ? null
            : Convert.ToString(value, CultureInfo.InvariantCulture);
    }

    private static async Task<long> ExecuteScalarInt64Async(
        SqliteConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        var value = await ExecuteScalarAsync(connection, sql, cancellationToken)
            .ConfigureAwait(false);

        return value is null or DBNull
            ? 0L
            : Convert.ToInt64(value, CultureInfo.InvariantCulture);
    }
}
