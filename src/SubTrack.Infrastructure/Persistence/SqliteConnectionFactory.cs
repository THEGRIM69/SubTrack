using Microsoft.Data.Sqlite;

namespace SubTrack.Infrastructure.Persistence;

/// <summary>
/// Crea conexiones SQLite con la configuración de integridad y concurrencia de SubTrack.
/// Es interna para que WinForms no reciba tipos de SQLite ni pueda ejecutar SQL directamente.
/// </summary>
internal sealed class SqliteConnectionFactory
{
    private readonly string _connectionString;
    private readonly int _busyTimeoutMilliseconds;

    public SqliteConnectionFactory(SubTrackDatabaseOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var normalized = options.Normalize();
        _busyTimeoutMilliseconds = normalized.BusyTimeoutMilliseconds;

        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = normalized.DatabasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Default,
            ForeignKeys = true,
            Pooling = true,
            DefaultTimeout = Math.Max(1, (int)Math.Ceiling(normalized.BusyTimeoutMilliseconds / 1000d))
        };

        _connectionString = builder.ToString();
    }

    public async Task<SqliteConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(_connectionString);

        try
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await ExecuteNonQueryAsync(
                connection,
                $"PRAGMA busy_timeout = {_busyTimeoutMilliseconds};",
                cancellationToken).ConfigureAwait(false);

            await ExecuteNonQueryAsync(
                connection,
                "PRAGMA foreign_keys = ON;",
                cancellationToken).ConfigureAwait(false);

            await ExecuteNonQueryAsync(
                connection,
                "PRAGMA synchronous = FULL;",
                cancellationToken).ConfigureAwait(false);

            return connection;
        }
        catch
        {
            await connection.DisposeAsync().ConfigureAwait(false);
            throw;
        }
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
}
