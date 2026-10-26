using Microsoft.Data.Sqlite;

namespace SubTrack.Infrastructure.Persistence.Repositories;

/// <summary>
/// Utilidades comunes de ADO.NET para no repetir la creación de comandos
/// ni la asignación de la transacción en cada consulta.
/// </summary>
internal static class SqliteCommandExtensions
{
    public static SqliteCommand CreateCommand(
        this SqliteConnection connection,
        SqliteTransaction? transaction,
        string sql)
    {
        var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = transaction;
        return command;
    }

    public static SqliteCommand WithParameters(
        this SqliteCommand command,
        params (string Name, object? Value)[] parameters)
    {
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }

        return command;
    }
}
