using Microsoft.Data.Sqlite;

namespace SubTrack.Infrastructure.Persistence.Repositories;

/// <summary>
/// F0-03 — Secuencias transaccionales. La numeración de comprobantes se
/// incrementa dentro de la misma transacción que inserta el pago, de modo
/// que un rollback no consume el número.
/// </summary>
internal sealed class SequenceRepository
{
    public async Task<long> NextAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string name,
        CancellationToken cancellationToken)
    {
        await using var command = connection
            .CreateCommand(
                transaction,
                """
                INSERT INTO secuencias (nombre, ultimo)
                VALUES ($nombre, 1)
                ON CONFLICT(nombre) DO UPDATE SET ultimo = ultimo + 1
                RETURNING ultimo;
                """)
            .WithParameters(("$nombre", name));

        var value = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

        return Convert.ToInt64(value);
    }
}
