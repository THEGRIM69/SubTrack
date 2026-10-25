using Microsoft.Data.Sqlite;
using SubTrack.Core.Domain.Entities;
using SubTrack.Core.Domain.Enums;
using SubTrack.Infrastructure.Persistence;

namespace SubTrack.Infrastructure.Persistence.Repositories;

/// <summary>
/// Acceso a pagos de suscripción. Los anulados se conservan siempre (F0-02).
/// </summary>
internal sealed class PaymentRepository
{
    private const string SelectPayments = """
        SELECT id, cobro_id, operacion_id, fecha_pago, monto_centavos, estado,
               numero_comprobante, anulado_en, motivo_anulacion, creado_en, actualizado_en
        FROM pagos
        """;

    public async Task<Payment?> GetByIdAsync(
        SqliteConnection connection,
        SqliteTransaction? transaction,
        Guid paymentId,
        CancellationToken cancellationToken)
    {
        await using var command = connection
            .CreateCommand(transaction, $"{SelectPayments} WHERE id = $id;")
            .WithParameters(("$id", paymentId.ToString()));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);

        return await reader.ReadAsync(cancellationToken).ConfigureAwait(false)
            ? Map(reader)
            : null;
    }

    public async Task<Payment?> GetByOperationIdAsync(
        SqliteConnection connection,
        SqliteTransaction? transaction,
        Guid operationId,
        CancellationToken cancellationToken)
    {
        await using var command = connection
            .CreateCommand(transaction, $"{SelectPayments} WHERE operacion_id = $operacion;")
            .WithParameters(("$operacion", operationId.ToString()));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);

        return await reader.ReadAsync(cancellationToken).ConfigureAwait(false)
            ? Map(reader)
            : null;
    }

    public async Task InsertAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Payment payment,
        CancellationToken cancellationToken)
    {
        await using var command = connection
            .CreateCommand(
                transaction,
                """
                INSERT INTO pagos
                    (id, cobro_id, operacion_id, fecha_pago, monto_centavos, estado,
                     numero_comprobante, anulado_en, motivo_anulacion, creado_en, actualizado_en)
                VALUES
                    ($id, $cobro, $operacion, $fecha, $monto, $estado,
                     $comprobante, $anulado, $motivo, $creado, $actualizado);
                """)
            .WithParameters(
                ("$id", payment.Id.ToString()),
                ("$cobro", payment.ScheduledChargeId.ToString()),
                ("$operacion", payment.OperationId.ToString()),
                ("$fecha", payment.PaymentDate.ToStorage()),
                ("$monto", payment.AmountCents),
                ("$estado", SqliteEnumCodes.ToCode(payment.Status)),
                ("$comprobante", payment.VoucherNumber),
                ("$anulado", payment.CancelledAt?.ToStorage()),
                ("$motivo", payment.CancellationReason),
                ("$creado", payment.CreatedAt.ToStorage()),
                ("$actualizado", payment.UpdatedAt.ToStorage()));

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task CancelAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Guid paymentId,
        DateTimeOffset cancelledAt,
        string reason,
        CancellationToken cancellationToken)
    {
        await using var command = connection
            .CreateCommand(
                transaction,
                """
                UPDATE pagos
                SET estado = 'ANULADO',
                    anulado_en = $anulado,
                    motivo_anulacion = $motivo,
                    actualizado_en = $actualizado
                WHERE id = $id AND estado = 'VIGENTE';
                """)
            .WithParameters(
                ("$id", paymentId.ToString()),
                ("$anulado", cancelledAt.ToStorage()),
                ("$motivo", reason),
                ("$actualizado", cancelledAt.ToStorage()));

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Payment>> GetHistoryAsync(
        SqliteConnection connection,
        int limit,
        int offset,
        CancellationToken cancellationToken)
    {
        await using var command = connection
            .CreateCommand(
                null,
                $"""
                {SelectPayments}
                ORDER BY fecha_pago DESC, creado_en DESC, numero_comprobante DESC
                LIMIT $limite OFFSET $desplazamiento;
                """)
            .WithParameters(("$limite", limit), ("$desplazamiento", offset));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);

        var payments = new List<Payment>();

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            payments.Add(Map(reader));
        }

        return payments;
    }

    public async Task<int> CountAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand(null, "SELECT COUNT(*) FROM pagos;");

        return Convert.ToInt32(
            await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false));
    }

    private static Payment Map(SqliteDataReader reader) => new()
    {
        Id = reader.GetGuid("id"),
        ScheduledChargeId = reader.GetGuid("cobro_id"),
        OperationId = reader.GetGuid("operacion_id"),
        PaymentDate = reader.GetDateOnly("fecha_pago"),
        AmountCents = reader.GetCents("monto_centavos"),
        Status = SqliteEnumCodes.ParsePaymentStatus(reader.GetText("estado")),
        VoucherNumber = reader.GetText("numero_comprobante"),
        CancelledAt = reader.GetNullableTimestamp("anulado_en"),
        CancellationReason = reader.GetNullableText("motivo_anulacion"),
        CreatedAt = reader.GetTimestamp("creado_en"),
        UpdatedAt = reader.GetTimestamp("actualizado_en")
    };
}
