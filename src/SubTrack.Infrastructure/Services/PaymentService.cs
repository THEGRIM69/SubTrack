using Microsoft.Data.Sqlite;
using SubTrack.Core.Abstractions;
using SubTrack.Core.Contracts;
using SubTrack.Core.Domain.Entities;
using SubTrack.Core.Domain.Enums;
using SubTrack.Core.Results;
using SubTrack.Infrastructure.Persistence;
using SubTrack.Infrastructure.Persistence.Repositories;

namespace SubTrack.Infrastructure.Services;

/// <summary>
/// F0-T03 — Implementación del registro y la anulación de pagos.
/// El importe nunca se recibe del formulario: se toma del cobro (F0-04).
/// </summary>
internal sealed class PaymentService : IPaymentService
{
    private readonly SqliteConnectionFactory _factory;
    private readonly SystemClock _clock;
    private readonly SubscriptionRepository _subscriptions = new();
    private readonly ScheduledChargeRepository _charges = new();
    private readonly PaymentRepository _payments = new();
    private readonly SequenceRepository _sequences = new();

    public PaymentService(SqliteConnectionFactory factory, SystemClock clock)
    {
        _factory = factory;
        _clock = clock;
    }

    public async Task<Result<Payment>> RegisterAsync(
        RegisterPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        var errors = ValidateRegister(request);

        if (errors.Count > 0)
        {
            return Result<Payment>.Fail(errors);
        }

        var today = _clock.Today;
        var now = _clock.UtcNow;

        await using var connection = await _factory.OpenAsync(cancellationToken)
            .ConfigureAwait(false);

        await using var transaction = connection.BeginTransaction(deferred: false);

        try
        {
            var existing = await _payments.GetByOperationIdAsync(
                connection,
                transaction,
                request.OperationId,
                cancellationToken).ConfigureAwait(false);

            if (existing is not null)
            {
                return await CompleteIdempotentAsync(connection, transaction, existing, cancellationToken)
                    .ConfigureAwait(false);
            }

            var charge = await _charges.GetByIdAsync(
                connection,
                transaction,
                request.ScheduledChargeId,
                cancellationToken).ConfigureAwait(false);

            if (charge is null)
            {
                return Result<Payment>.Fail(
                    ErrorCodes.CobroNoEncontrado,
                    "El cobro no existe.");
            }

            if (charge.Status == ScheduledChargeStatus.Paid)
            {
                return Result<Payment>.Fail(
                    ErrorCodes.CobroYaPagado,
                    "Este cobro ya fue registrado como pagado.");
            }

            if (charge.Status == ScheduledChargeStatus.Cancelled)
            {
                return Result<Payment>.Fail(
                    ErrorCodes.CobroCancelado,
                    "El cobro está cancelado y no admite pagos.");
            }

            // C-07: el comprobante se numera por el año de la fecha de pago.
            var year = request.PaymentDate.Year;
            var sequence = await _sequences.NextAsync(
                connection,
                transaction,
                $"comprobante_pago:{year:D4}",
                cancellationToken).ConfigureAwait(false);

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                ScheduledChargeId = charge.Id,
                OperationId = request.OperationId,
                PaymentDate = request.PaymentDate,
                AmountCents = charge.AmountCents,
                Status = PaymentStatus.Active,
                VoucherNumber = $"CMP-{year:D4}-{sequence:D6}",
                CancelledAt = null,
                CancellationReason = null,
                CreatedAt = now,
                UpdatedAt = now
            };

            await _payments.InsertAsync(connection, transaction, payment, cancellationToken)
                .ConfigureAwait(false);

            await _charges.UpdateStatusAsync(
                connection,
                transaction,
                charge.Id,
                ScheduledChargeStatus.Paid,
                now,
                cancellationToken).ConfigureAwait(false);

            await UpdateNextChargeAsync(
                connection,
                transaction,
                charge,
                now,
                cancellationToken).ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return Result<Payment>.Ok(payment);
        }
        catch (SqliteException exception) when (SqliteErrorTranslator.IsUniqueViolation(exception))
        {
            await ServiceSupport.SafeRollbackAsync(transaction, cancellationToken)
                .ConfigureAwait(false);

            var existing = await _payments.GetByOperationIdAsync(
                connection,
                null,
                request.OperationId,
                cancellationToken).ConfigureAwait(false);

            if (existing is not null)
            {
                return existing.Status == PaymentStatus.Cancelled
                    ? Result<Payment>.Fail(
                        ErrorCodes.OperacionYaProcesada,
                        "Esa operación ya fue registrada y luego anulada. Use un nuevo intento.")
                    : Result<Payment>.Ok(existing);
            }

            return Result<Payment>.Fail(
                ErrorCodes.CobroYaPagado,
                "Este cobro ya tiene un pago vigente.");
        }
        catch (SqliteException exception)
        {
            await ServiceSupport.SafeRollbackAsync(transaction, cancellationToken)
                .ConfigureAwait(false);

            return Result<Payment>.Fail(SqliteErrorTranslator.Translate(exception));
        }
    }

    public async Task<Result<Payment>> CancelAsync(
        CancelPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (DomainValidation.Reason(request.Reason) is { } reasonError)
        {
            return Result<Payment>.Fail(reasonError);
        }

        var now = _clock.UtcNow;

        await using var connection = await _factory.OpenAsync(cancellationToken)
            .ConfigureAwait(false);

        await using var transaction = connection.BeginTransaction(deferred: false);

        try
        {
            var payment = await _payments.GetByIdAsync(
                connection,
                transaction,
                request.PaymentId,
                cancellationToken).ConfigureAwait(false);

            if (payment is null)
            {
                return Result<Payment>.Fail(
                    ErrorCodes.PagoNoEncontrado,
                    "El pago no existe.");
            }

            if (payment.Status == PaymentStatus.Cancelled)
            {
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return Result<Payment>.Ok(payment);
            }

            var reason = request.Reason.Trim();

            await _payments.CancelAsync(
                connection,
                transaction,
                payment.Id,
                now,
                reason,
                cancellationToken).ConfigureAwait(false);

            var charge = await _charges.GetByIdAsync(
                connection,
                transaction,
                payment.ScheduledChargeId,
                cancellationToken).ConfigureAwait(false);

            if (charge is not null)
            {
                var hasRemainingActivePayment = await _payments.HasActivePaymentForChargeAsync(
                    connection,
                    transaction,
                    charge.Id,
                    cancellationToken).ConfigureAwait(false);

                await _charges.UpdateStatusAsync(
                    connection,
                    transaction,
                    charge.Id,
                    hasRemainingActivePayment
                        ? ScheduledChargeStatus.Paid
                        : ScheduledChargeStatus.Pending,
                    now,
                    cancellationToken).ConfigureAwait(false);

                await UpdateNextChargeAsync(
                    connection,
                    transaction,
                    charge,
                    now,
                    cancellationToken).ConfigureAwait(false);
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return Result<Payment>.Ok(payment with
            {
                Status = PaymentStatus.Cancelled,
                CancelledAt = now,
                CancellationReason = reason,
                UpdatedAt = now
            });
        }
        catch (SqliteException exception)
        {
            await ServiceSupport.SafeRollbackAsync(transaction, cancellationToken)
                .ConfigureAwait(false);

            return Result<Payment>.Fail(SqliteErrorTranslator.Translate(exception));
        }
    }

    public async Task<Result<Payment>> GetByIdAsync(
        Guid paymentId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _factory.OpenAsync(cancellationToken)
            .ConfigureAwait(false);

        var payment = await _payments.GetByIdAsync(connection, null, paymentId, cancellationToken)
            .ConfigureAwait(false);

        return payment is null
            ? Result<Payment>.Fail(ErrorCodes.PagoNoEncontrado, "El pago no existe.")
            : Result<Payment>.Ok(payment);
    }

    public async Task<Result<IReadOnlyList<Payment>>> GetByScheduledChargeIdAsync(
        Guid scheduledChargeId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _factory.OpenAsync(cancellationToken)
            .ConfigureAwait(false);

        var charge = await _charges.GetByIdAsync(
            connection,
            null,
            scheduledChargeId,
            cancellationToken).ConfigureAwait(false);

        if (charge is null)
        {
            return Result<IReadOnlyList<Payment>>.Fail(
                ErrorCodes.CobroNoEncontrado,
                "El cobro no existe.");
        }

        var payments = await _payments.GetByScheduledChargeIdAsync(
            connection,
            scheduledChargeId,
            cancellationToken).ConfigureAwait(false);

        return Result<IReadOnlyList<Payment>>.Ok(payments);
    }

    public async Task<Result<PagedResult<Payment>>> GetHistoryAsync(
        PageRequest page,
        CancellationToken cancellationToken = default)
    {
        if (!ServiceSupport.IsValidPage(page))
        {
            return Result<PagedResult<Payment>>.Fail(
                ErrorCodes.RangoExcedido,
                $"El tamaño de página debe estar entre 1 y {PageRequest.MaxPageSize}.");
        }

        await using var connection = await _factory.OpenAsync(cancellationToken)
            .ConfigureAwait(false);

        var total = await _payments.CountAsync(connection, cancellationToken).ConfigureAwait(false);

        var items = await _payments.GetHistoryAsync(
            connection,
            page.PageSize,
            (page.Page - 1) * page.PageSize,
            cancellationToken).ConfigureAwait(false);

        return Result<PagedResult<Payment>>.Ok(
            new PagedResult<Payment>(items, page.Page, page.PageSize, total));
    }

    private async Task<Result<Payment>> CompleteIdempotentAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Payment existing,
        CancellationToken cancellationToken)
    {
        if (existing.Status == PaymentStatus.Cancelled)
        {
            return Result<Payment>.Fail(
                ErrorCodes.OperacionYaProcesada,
                "Esa operación ya fue registrada y luego anulada. Use un nuevo intento.");
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return Result<Payment>.Ok(existing);
    }

    private async Task UpdateNextChargeAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        ScheduledCharge charge,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var subscription = await _subscriptions.GetByIdAsync(
            connection,
            transaction,
            charge.SubscriptionId,
            cancellationToken).ConfigureAwait(false);

        if (subscription is null)
        {
            return;
        }

        var pending = await _charges.GetMinPendingDateAsync(
            connection,
            transaction,
            subscription.Id,
            cancellationToken).ConfigureAwait(false);

        if (pending is null && subscription.IsActive)
        {
            var versions = await _subscriptions.GetVersionsAsync(
                connection,
                transaction,
                subscription.Id,
                cancellationToken).ConfigureAwait(false);

            if (versions.Count > 0)
            {
                pending = ChargeScheduling.NextDateAfter(versions, charge.ScheduledDate);
            }
        }

        await _subscriptions.UpdateNextChargeAsync(
            connection,
            transaction,
            subscription.Id,
            pending,
            now,
            cancellationToken).ConfigureAwait(false);
    }

    private List<DomainError> ValidateRegister(RegisterPaymentRequest request)
    {
        var errors = new List<DomainError>();

        if (request.OperationId == Guid.Empty)
        {
            errors.Add(new DomainError(
                ErrorCodes.OperacionInvalida,
                "La operación no es válida. Vuelva a intentarlo.",
                "OperationId"));
        }

        if (request.ScheduledChargeId == Guid.Empty)
        {
            errors.Add(new DomainError(
                ErrorCodes.CobroNoEncontrado,
                "Debe seleccionar un cobro.",
                "ScheduledChargeId"));
        }

        if (request.PaymentDate == default)
        {
            errors.Add(new DomainError(
                ErrorCodes.FechaRequerida,
                "Debe indicar la fecha de pago.",
                "PaymentDate"));
        }
        else if (request.PaymentDate > _clock.Today)
        {
            // C-04: no se puede registrar un pago en el futuro.
            errors.Add(new DomainError(
                ErrorCodes.FechaFuturaNoPermitida,
                "La fecha de pago no puede ser posterior a hoy.",
                "PaymentDate"));
        }

        return errors;
    }
}
