using Microsoft.Data.Sqlite;
using SubTrack.Core.Domain.Entities;
using SubTrack.Core.Domain.Enums;
using SubTrack.Infrastructure.Persistence;

namespace SubTrack.Infrastructure.Persistence.Repositories;

/// <summary>
/// Acceso a cobros programados. La fecha programada nunca se modifica (F0-05);
/// solo cambian el importe estimado y el estado.
/// </summary>
internal sealed class ScheduledChargeRepository
{
    private const string SelectCharges = """
        SELECT id, suscripcion_id, fecha_programada, monto_centavos,
               estado, creado_en, actualizado_en
        FROM cobros_programados
        """;

    public async Task<ScheduledCharge?> GetLastPaidWithActivePaymentAsync(
        SqliteConnection connection,
        SqliteTransaction? transaction,
        Guid subscriptionId,
        CancellationToken cancellationToken)
    {
        await using var command = connection
            .CreateCommand(
                transaction,
                $"""
                {SelectCharges}
                WHERE suscripcion_id = $suscripcion
                  AND estado = 'PAGADO'
                  AND EXISTS (
                      SELECT 1 FROM pagos p
                      WHERE p.cobro_id = cobros_programados.id
                        AND p.estado = 'VIGENTE'
                  )
                ORDER BY fecha_programada DESC
                LIMIT 1;
                """)
            .WithParameters(("$suscripcion", subscriptionId.ToString()));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);

        return await reader.ReadAsync(cancellationToken).ConfigureAwait(false)
            ? Map(reader)
            : null;
    }

    public async Task<ScheduledCharge?> GetByIdAsync(
        SqliteConnection connection,
        SqliteTransaction? transaction,
        Guid chargeId,
        CancellationToken cancellationToken)
    {
        await using var command = connection
            .CreateCommand(transaction, $"{SelectCharges} WHERE id = $id;")
            .WithParameters(("$id", chargeId.ToString()));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);

        return await reader.ReadAsync(cancellationToken).ConfigureAwait(false)
            ? Map(reader)
            : null;
    }

    public async Task<IReadOnlyList<ScheduledCharge>> GetByRangeAsync(
        SqliteConnection connection,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken)
    {
        await using var command = connection
            .CreateCommand(
                null,
                $"""
                {SelectCharges}
                WHERE fecha_programada BETWEEN $desde AND $hasta
                  AND estado IN ('PENDIENTE', 'PAGADO')
                ORDER BY fecha_programada ASC;
                """)
            .WithParameters(("$desde", from.ToStorage()), ("$hasta", to.ToStorage()));

        return await ReadListAsync(command, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ScheduledCharge>> GetPendingAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand(
            null,
            $"{SelectCharges} WHERE estado = 'PENDIENTE' ORDER BY fecha_programada ASC;");

        return await ReadListAsync(command, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ScheduledCharge>> GetPendingInRangeAsync(
        SqliteConnection connection,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken)
    {
        await using var command = connection
            .CreateCommand(
                null,
                $"""
                {SelectCharges}
                WHERE estado = 'PENDIENTE' AND fecha_programada BETWEEN $desde AND $hasta
                ORDER BY fecha_programada ASC;
                """)
            .WithParameters(("$desde", from.ToStorage()), ("$hasta", to.ToStorage()));

        return await ReadListAsync(command, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ScheduledCharge>> GetOverdueAsync(
        SqliteConnection connection,
        DateOnly asOf,
        CancellationToken cancellationToken)
    {        await using var command = connection
            .CreateCommand(
                null,
                $"""
                {SelectCharges}
                WHERE estado = 'PENDIENTE' AND fecha_programada < $hoy
                ORDER BY fecha_programada ASC;
                """)
            .WithParameters(("$hoy", asOf.ToStorage()));

        return await ReadListAsync(command, cancellationToken).ConfigureAwait(false);
    }

    public async Task<DateOnly?> GetMinPendingDateAsync(
        SqliteConnection connection,
        SqliteTransaction? transaction,
        Guid subscriptionId,
        CancellationToken cancellationToken)
    {
        await using var command = connection
            .CreateCommand(
                transaction,
                """
                SELECT MIN(fecha_programada)
                FROM cobros_programados
                WHERE suscripcion_id = $suscripcion AND estado = 'PENDIENTE';
                """)
            .WithParameters(("$suscripcion", subscriptionId.ToString()));

        var value = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

        return value is null or DBNull
            ? null
            : DateOnly.ParseExact((string)value, SqliteDataReaderExtensions.DateFormat);
    }

    public async Task<DateOnly?> GetMaxScheduledDateAsync(
        SqliteConnection connection,
        SqliteTransaction? transaction,
        Guid subscriptionId,
        CancellationToken cancellationToken)
    {
        await using var command = connection
            .CreateCommand(
                transaction,
                """
                SELECT MAX(fecha_programada)
                FROM cobros_programados
                WHERE suscripcion_id = $suscripcion;
                """)
            .WithParameters(("$suscripcion", subscriptionId.ToString()));

        var value = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

        return value is null or DBNull
            ? null
            : DateOnly.ParseExact((string)value, SqliteDataReaderExtensions.DateFormat);
    }

    public async Task<bool> ExistsAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Guid subscriptionId,
        DateOnly scheduledDate,
        CancellationToken cancellationToken)
    {
        await using var command = connection
            .CreateCommand(
                transaction,
                """
                SELECT 1 FROM cobros_programados
                WHERE suscripcion_id = $suscripcion AND fecha_programada = $fecha
                LIMIT 1;
                """)
            .WithParameters(
                ("$suscripcion", subscriptionId.ToString()),
                ("$fecha", scheduledDate.ToStorage()));

        return await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) is not null;
    }

    public async Task<bool> HasPaymentsAsync(
        SqliteConnection connection,
        SqliteTransaction? transaction,
        Guid chargeId,
        CancellationToken cancellationToken)
    {
        await using var command = connection
            .CreateCommand(
                transaction,
                "SELECT 1 FROM pagos WHERE cobro_id = $cobro LIMIT 1;")
            .WithParameters(("$cobro", chargeId.ToString()));

        return await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) is not null;
    }

    public async Task InsertAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        ScheduledCharge charge,
        CancellationToken cancellationToken)
    {
        await using var command = connection
            .CreateCommand(
                transaction,
                """
                INSERT INTO cobros_programados
                    (id, suscripcion_id, fecha_programada, monto_centavos, estado,
                     creado_en, actualizado_en)
                VALUES
                    ($id, $suscripcion, $fecha, $monto, $estado, $creado, $actualizado);
                """)
            .WithParameters(
                ("$id", charge.Id.ToString()),
                ("$suscripcion", charge.SubscriptionId.ToString()),
                ("$fecha", charge.ScheduledDate.ToStorage()),
                ("$monto", charge.AmountCents),
                ("$estado", SqliteEnumCodes.ToCode(charge.Status)),
                ("$creado", charge.CreatedAt.ToStorage()),
                ("$actualizado", charge.UpdatedAt.ToStorage()));

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateStatusAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Guid chargeId,
        ScheduledChargeStatus status,
        DateTimeOffset updatedAt,
        CancellationToken cancellationToken)
    {
        await using var command = connection
            .CreateCommand(
                transaction,
                """
                UPDATE cobros_programados
                SET estado = $estado, actualizado_en = $actualizado
                WHERE id = $id;
                """)
            .WithParameters(
                ("$id", chargeId.ToString()),
                ("$estado", SqliteEnumCodes.ToCode(status)),
                ("$actualizado", updatedAt.ToStorage()));

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAmountAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Guid chargeId,
        long amountCents,
        DateTimeOffset updatedAt,
        CancellationToken cancellationToken)
    {
        await using var command = connection
            .CreateCommand(
                transaction,
                """
                UPDATE cobros_programados
                SET monto_centavos = $monto, actualizado_en = $actualizado
                WHERE id = $id;
                """)
            .WithParameters(
                ("$id", chargeId.ToString()),
                ("$monto", amountCents),
                ("$actualizado", updatedAt.ToStorage()));

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// F0-T06 — Elimina solo cobros futuros pendientes sin ningún pago asociado.
    /// Los cobros pagados o con pagos anulados conservan el historial.
    /// </summary>
    public async Task<int> DeleteFuturePendingWithoutPaymentsAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Guid subscriptionId,
        DateOnly fromDate,
        CancellationToken cancellationToken)
    {
        await using var command = connection
            .CreateCommand(
                transaction,
                """
                DELETE FROM cobros_programados
                WHERE suscripcion_id = $suscripcion
                  AND fecha_programada >= $desde
                  AND estado = 'PENDIENTE'
                  AND NOT EXISTS (SELECT 1 FROM pagos p WHERE p.cobro_id = cobros_programados.id);
                """)
            .WithParameters(
                ("$suscripcion", subscriptionId.ToString()),
                ("$desde", fromDate.ToStorage()));

        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<IReadOnlyList<ScheduledCharge>> ReadListAsync(
        SqliteCommand command,
        CancellationToken cancellationToken)
    {
        await using var reader = await command.ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);

        var charges = new List<ScheduledCharge>();

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            charges.Add(Map(reader));
        }

        return charges;
    }

    private static ScheduledCharge Map(SqliteDataReader reader) => new()
    {
        Id = reader.GetGuid("id"),
        SubscriptionId = reader.GetGuid("suscripcion_id"),
        ScheduledDate = reader.GetDateOnly("fecha_programada"),
        AmountCents = reader.GetCents("monto_centavos"),
        Status = SqliteEnumCodes.ParseChargeStatus(reader.GetText("estado")),
        CreatedAt = reader.GetTimestamp("creado_en"),
        UpdatedAt = reader.GetTimestamp("actualizado_en")
    };
}
