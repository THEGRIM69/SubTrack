using Microsoft.Data.Sqlite;
using SubTrack.Core.Domain.Entities;
using SubTrack.Infrastructure.Persistence;

namespace SubTrack.Infrastructure.Persistence.Repositories;

/// <summary>
/// Acceso al saldo inicial único y a su historial (F0-20, F0-21).
/// </summary>
internal sealed class OpeningBalanceRepository
{
    public const string FixedId = "saldo_inicial";

    public async Task<OpeningBalance?> GetAsync(
        SqliteConnection connection,
        SqliteTransaction? transaction,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand(
            transaction,
            """
            SELECT monto_centavos, fecha_inicio, creado_en, actualizado_en
            FROM saldo_inicial
            WHERE id = 'saldo_inicial';
            """);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);

        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        return new OpeningBalance
        {
            AmountCents = reader.GetCents("monto_centavos"),
            StartDate = reader.GetDateOnly("fecha_inicio"),
            CreatedAt = reader.GetTimestamp("creado_en"),
            UpdatedAt = reader.GetTimestamp("actualizado_en")
        };
    }

    public async Task InsertAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        OpeningBalance balance,
        CancellationToken cancellationToken)
    {
        await using var command = connection
            .CreateCommand(
                transaction,
                """
                INSERT INTO saldo_inicial
                    (id, monto_centavos, fecha_inicio, creado_en, actualizado_en)
                VALUES
                    ('saldo_inicial', $monto, $inicio, $creado, $actualizado);
                """)
            .WithParameters(
                ("$monto", balance.AmountCents),
                ("$inicio", balance.StartDate.ToStorage()),
                ("$creado", balance.CreatedAt.ToStorage()),
                ("$actualizado", balance.UpdatedAt.ToStorage()));

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        OpeningBalance balance,
        CancellationToken cancellationToken)
    {
        await using var command = connection
            .CreateCommand(
                transaction,
                """
                UPDATE saldo_inicial
                SET monto_centavos = $monto,
                    fecha_inicio = $inicio,
                    actualizado_en = $actualizado
                WHERE id = 'saldo_inicial';
                """)
            .WithParameters(
                ("$monto", balance.AmountCents),
                ("$inicio", balance.StartDate.ToStorage()),
                ("$actualizado", balance.UpdatedAt.ToStorage()));

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task InsertCorrectionAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        OpeningBalanceCorrection correction,
        CancellationToken cancellationToken)
    {
        await using var command = connection
            .CreateCommand(
                transaction,
                """
                INSERT INTO saldo_inicial_historial
                    (id, saldo_inicial_id, monto_anterior, monto_nuevo,
                     fecha_inicio_anterior, fecha_inicio_nueva, modificado_en)
                VALUES
                    ($id, 'saldo_inicial', $montoAnterior, $montoNuevo,
                     $fechaAnterior, $fechaNueva, $modificado);
                """)
            .WithParameters(
                ("$id", correction.Id.ToString()),
                ("$montoAnterior", correction.PreviousAmountCents),
                ("$montoNuevo", correction.NewAmountCents),
                ("$fechaAnterior", correction.PreviousStartDate.ToStorage()),
                ("$fechaNueva", correction.NewStartDate.ToStorage()),
                ("$modificado", correction.ModifiedAt.ToStorage()));

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<OpeningBalanceCorrection>> GetCorrectionsAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand(
            null,
            """
            SELECT id, monto_anterior, monto_nuevo, fecha_inicio_anterior,
                   fecha_inicio_nueva, modificado_en
            FROM saldo_inicial_historial
            ORDER BY modificado_en DESC;
            """);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);

        var corrections = new List<OpeningBalanceCorrection>();

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            corrections.Add(new OpeningBalanceCorrection
            {
                Id = reader.GetGuid("id"),
                PreviousAmountCents = reader.GetCents("monto_anterior"),
                NewAmountCents = reader.GetCents("monto_nuevo"),
                PreviousStartDate = reader.GetDateOnly("fecha_inicio_anterior"),
                NewStartDate = reader.GetDateOnly("fecha_inicio_nueva"),
                ModifiedAt = reader.GetTimestamp("modificado_en")
            });
        }

        return corrections;
    }
}
