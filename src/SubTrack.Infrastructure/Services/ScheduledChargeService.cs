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
/// F0-T03 — Implementación de la generación y el ajuste de cobros programados.
/// </summary>
internal sealed class ScheduledChargeService : IScheduledChargeService
{
    private const int MaxHorizonMonths = 12;

    private readonly SqliteConnectionFactory _factory;
    private readonly SystemClock _clock;
    private readonly SubscriptionRepository _subscriptions = new();
    private readonly ScheduledChargeRepository _charges = new();

    public ScheduledChargeService(SqliteConnectionFactory factory, SystemClock clock)
    {
        _factory = factory;
        _clock = clock;
    }

    public async Task<Result<IReadOnlyList<ScheduledCharge>>> GenerateAsync(
        GenerateChargesRequest request,
        CancellationToken cancellationToken = default)
    {
        var today = _clock.Today;

        if (request.Until < today)
        {
            return Result<IReadOnlyList<ScheduledCharge>>.Fail(
                ErrorCodes.RangoExcedido,
                "La fecha límite debe ser hoy o posterior.");
        }

        if (request.Until > today.AddMonths(MaxHorizonMonths))
        {
            return Result<IReadOnlyList<ScheduledCharge>>.Fail(
                ErrorCodes.RangoExcedido,
                $"Solo se pueden generar cobros con hasta {MaxHorizonMonths} meses de anticipación.");
        }

        var now = _clock.UtcNow;

        await using var connection = await _factory.OpenAsync(cancellationToken)
            .ConfigureAwait(false);

        await using var transaction = connection.BeginTransaction(deferred: false);

        try
        {
            var subscription = await _subscriptions.GetByIdAsync(
                connection,
                transaction,
                request.SubscriptionId,
                cancellationToken).ConfigureAwait(false);

            if (subscription is null)
            {
                return Result<IReadOnlyList<ScheduledCharge>>.Fail(
                    ErrorCodes.SuscripcionNoEncontrada,
                    "La suscripción no existe.");
            }

            if (!subscription.IsActive)
            {
                return Result<IReadOnlyList<ScheduledCharge>>.Fail(
                    ErrorCodes.SuscripcionInactiva,
                    "La suscripción está desactivada; no se generan cobros nuevos.");
            }

            var versions = await _subscriptions.GetVersionsAsync(
                connection,
                transaction,
                subscription.Id,
                cancellationToken).ConfigureAwait(false);

            var created = new List<ScheduledCharge>();

            foreach (var plan in ChargeScheduling.PlanSegments(
                versions,
                today,
                request.Until,
                subscription.DeactivatedOn))
            {
                if (await _charges.ExistsAsync(
                    connection,
                    transaction,
                    subscription.Id,
                    plan.Date,
                    cancellationToken).ConfigureAwait(false))
                {
                    continue;
                }

                var charge = new ScheduledCharge
                {
                    Id = Guid.NewGuid(),
                    SubscriptionId = subscription.Id,
                    ScheduledDate = plan.Date,
                    AmountCents = plan.Version.AmountCents,
                    Status = ScheduledChargeStatus.Pending,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                await _charges.InsertAsync(connection, transaction, charge, cancellationToken)
                    .ConfigureAwait(false);

                created.Add(charge);
            }

            var next = await ResolveNextAsync(
                connection,
                transaction,
                subscription,
                versions,
                cancellationToken).ConfigureAwait(false);

            await _subscriptions.UpdateNextChargeAsync(
                connection,
                transaction,
                subscription.Id,
                next,
                now,
                cancellationToken).ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return Result<IReadOnlyList<ScheduledCharge>>.Ok(created);
        }
        catch (SqliteException exception) when (SqliteErrorTranslator.IsUniqueViolation(exception))
        {
            await ServiceSupport.SafeRollbackAsync(transaction, cancellationToken)
                .ConfigureAwait(false);

            return Result<IReadOnlyList<ScheduledCharge>>.Fail(
                ErrorCodes.CobroDuplicado,
                "Alguno de los cobros ya existía. Actualice la lista e intente de nuevo.");
        }
        catch (SqliteException exception)
        {
            await ServiceSupport.SafeRollbackAsync(transaction, cancellationToken)
                .ConfigureAwait(false);

            return Result<IReadOnlyList<ScheduledCharge>>.Fail(
                SqliteErrorTranslator.Translate(exception));
        }
    }

    public async Task<Result<IReadOnlyList<ScheduledCharge>>> GetByRangeAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        if (to < from)
        {
            return Result<IReadOnlyList<ScheduledCharge>>.Fail(
                ErrorCodes.RangoExcedido,
                "La fecha final no puede ser anterior a la inicial.");
        }

        await using var connection = await _factory.OpenAsync(cancellationToken)
            .ConfigureAwait(false);

        var charges = await _charges.GetByRangeAsync(connection, from, to, cancellationToken)
            .ConfigureAwait(false);

        return Result<IReadOnlyList<ScheduledCharge>>.Ok(charges);
    }

    public async Task<Result<IReadOnlyList<ScheduledCharge>>> GetPendingAsync(
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _factory.OpenAsync(cancellationToken)
            .ConfigureAwait(false);

        var charges = await _charges.GetPendingAsync(connection, cancellationToken)
            .ConfigureAwait(false);

        return Result<IReadOnlyList<ScheduledCharge>>.Ok(charges);
    }

    public async Task<Result<IReadOnlyList<ScheduledCharge>>> GetOverdueAsync(
        DateOnly asOf,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _factory.OpenAsync(cancellationToken)
            .ConfigureAwait(false);

        var charges = await _charges.GetOverdueAsync(connection, asOf, cancellationToken)
            .ConfigureAwait(false);

        return Result<IReadOnlyList<ScheduledCharge>>.Ok(charges);
    }

    public async Task<Result<ScheduledCharge>> AdjustAmountAsync(
        AdjustChargeAmountRequest request,
        CancellationToken cancellationToken = default)
    {
        if (DomainValidation.Amount(request.AmountCents) is { } amountError)
        {
            return Result<ScheduledCharge>.Fail(amountError);
        }

        var now = _clock.UtcNow;

        await using var connection = await _factory.OpenAsync(cancellationToken)
            .ConfigureAwait(false);

        await using var transaction = connection.BeginTransaction(deferred: false);

        try
        {
            var charge = await _charges.GetByIdAsync(
                connection,
                transaction,
                request.ScheduledChargeId,
                cancellationToken).ConfigureAwait(false);

            if (charge is null)
            {
                return Result<ScheduledCharge>.Fail(
                    ErrorCodes.CobroNoEncontrado,
                    "El cobro no existe.");
            }

            if (charge.Status == ScheduledChargeStatus.Paid)
            {
                return Result<ScheduledCharge>.Fail(
                    ErrorCodes.CobroYaPagado,
                    "El cobro ya fue pagado y no admite cambios de importe.");
            }

            if (charge.Status == ScheduledChargeStatus.Cancelled)
            {
                return Result<ScheduledCharge>.Fail(
                    ErrorCodes.CobroCancelado,
                    "El cobro está cancelado y no admite cambios de importe.");
            }

            var subscription = await _subscriptions.GetByIdAsync(
                connection,
                transaction,
                charge.SubscriptionId,
                cancellationToken).ConfigureAwait(false);

            if (subscription is null)
            {
                return Result<ScheduledCharge>.Fail(
                    ErrorCodes.SuscripcionNoEncontrada,
                    "La suscripción del cobro no existe.");
            }

            if (subscription.AmountType != AmountType.Variable)
            {
                return Result<ScheduledCharge>.Fail(
                    ErrorCodes.AjusteNoPermitido,
                    "Solo las suscripciones de importe variable pueden ajustar el cobro.");
            }

            await _charges.UpdateAmountAsync(
                connection,
                transaction,
                charge.Id,
                request.AmountCents,
                now,
                cancellationToken).ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return Result<ScheduledCharge>.Ok(charge with
            {
                AmountCents = request.AmountCents,
                UpdatedAt = now
            });
        }
        catch (SqliteException exception)
        {
            await ServiceSupport.SafeRollbackAsync(transaction, cancellationToken)
                .ConfigureAwait(false);

            return Result<ScheduledCharge>.Fail(SqliteErrorTranslator.Translate(exception));
        }
    }

    public async Task<Result<ScheduledCharge>> RegisterInitialPendingAsync(
        RegisterInitialPendingChargeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (DomainValidation.Amount(request.AmountCents) is { } amountError)
        {
            return Result<ScheduledCharge>.Fail(amountError);
        }

        if (request.ScheduledDate == default)
        {
            return Result<ScheduledCharge>.Fail(
                ErrorCodes.FechaRequerida,
                "Debe indicar la fecha del cobro anterior.");
        }

        var now = _clock.UtcNow;

        await using var connection = await _factory.OpenAsync(cancellationToken)
            .ConfigureAwait(false);

        await using var transaction = connection.BeginTransaction(deferred: false);

        try
        {
            var subscription = await _subscriptions.GetByIdAsync(
                connection,
                transaction,
                request.SubscriptionId,
                cancellationToken).ConfigureAwait(false);

            if (subscription is null)
            {
                return Result<ScheduledCharge>.Fail(
                    ErrorCodes.SuscripcionNoEncontrada,
                    "La suscripción no existe.");
            }

            if (await _charges.ExistsAsync(
                connection,
                transaction,
                subscription.Id,
                request.ScheduledDate,
                cancellationToken).ConfigureAwait(false))
            {
                return Result<ScheduledCharge>.Fail(
                    ErrorCodes.CobroDuplicado,
                    "Ya existe un cobro para esa suscripción y fecha.");
            }

            var charge = new ScheduledCharge
            {
                Id = Guid.NewGuid(),
                SubscriptionId = subscription.Id,
                ScheduledDate = request.ScheduledDate,
                AmountCents = request.AmountCents,
                Status = ScheduledChargeStatus.Pending,
                CreatedAt = now,
                UpdatedAt = now
            };

            await _charges.InsertAsync(connection, transaction, charge, cancellationToken)
                .ConfigureAwait(false);

            var next = await _charges.GetMinPendingDateAsync(
                connection,
                transaction,
                subscription.Id,
                cancellationToken).ConfigureAwait(false);

            await _subscriptions.UpdateNextChargeAsync(
                connection,
                transaction,
                subscription.Id,
                next,
                now,
                cancellationToken).ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return Result<ScheduledCharge>.Ok(charge);
        }
        catch (SqliteException exception)
        {
            await ServiceSupport.SafeRollbackAsync(transaction, cancellationToken)
                .ConfigureAwait(false);

            return Result<ScheduledCharge>.Fail(SqliteErrorTranslator.Translate(exception));
        }
    }

    public async Task<Result<ScheduledCharge>> CancelAsync(
        Guid scheduledChargeId,
        CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;

        await using var connection = await _factory.OpenAsync(cancellationToken)
            .ConfigureAwait(false);

        await using var transaction = connection.BeginTransaction(deferred: false);

        try
        {
            var charge = await _charges.GetByIdAsync(
                connection,
                transaction,
                scheduledChargeId,
                cancellationToken).ConfigureAwait(false);

            if (charge is null)
            {
                return Result<ScheduledCharge>.Fail(
                    ErrorCodes.CobroNoEncontrado,
                    "El cobro no existe.");
            }

            if (charge.Status == ScheduledChargeStatus.Cancelled)
            {
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return Result<ScheduledCharge>.Ok(charge);
            }

            if (charge.Status == ScheduledChargeStatus.Paid)
            {
                return Result<ScheduledCharge>.Fail(
                    ErrorCodes.CobroYaPagado,
                    "No se puede cancelar un cobro pagado. Anule primero el pago.");
            }

            if (await _charges.HasPaymentsAsync(
                connection,
                transaction,
                charge.Id,
                cancellationToken).ConfigureAwait(false))
            {
                return Result<ScheduledCharge>.Fail(
                    ErrorCodes.CobroConPagos,
                    "El cobro tiene pagos asociados y no puede cancelarse.");
            }

            await _charges.UpdateStatusAsync(
                connection,
                transaction,
                charge.Id,
                ScheduledChargeStatus.Cancelled,
                now,
                cancellationToken).ConfigureAwait(false);

            var subscription = await _subscriptions.GetByIdAsync(
                connection,
                transaction,
                charge.SubscriptionId,
                cancellationToken).ConfigureAwait(false);

            if (subscription is not null)
            {
                var versions = await _subscriptions.GetVersionsAsync(
                    connection,
                    transaction,
                    subscription.Id,
                    cancellationToken).ConfigureAwait(false);

                var next = await _charges.GetMinPendingDateAsync(
                    connection,
                    transaction,
                    subscription.Id,
                    cancellationToken).ConfigureAwait(false)
                    ?? ChargeScheduling.NextDateAfter(versions, charge.ScheduledDate);

                await _subscriptions.UpdateNextChargeAsync(
                    connection,
                    transaction,
                    subscription.Id,
                    next,
                    now,
                    cancellationToken).ConfigureAwait(false);
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return Result<ScheduledCharge>.Ok(charge with
            {
                Status = ScheduledChargeStatus.Cancelled,
                UpdatedAt = now
            });
        }
        catch (SqliteException exception)
        {
            await ServiceSupport.SafeRollbackAsync(transaction, cancellationToken)
                .ConfigureAwait(false);

            return Result<ScheduledCharge>.Fail(SqliteErrorTranslator.Translate(exception));
        }
    }

    private async Task<DateOnly?> ResolveNextAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Subscription subscription,
        IReadOnlyList<SubscriptionVersion> versions,
        CancellationToken cancellationToken)
    {
        if (!subscription.IsActive)
        {
            return await _charges.GetMinPendingDateAsync(
                connection,
                transaction,
                subscription.Id,
                cancellationToken).ConfigureAwait(false);
        }

        var pending = await _charges.GetMinPendingDateAsync(
            connection,
            transaction,
            subscription.Id,
            cancellationToken).ConfigureAwait(false);

        if (pending is not null)
        {
            return pending;
        }

        var lastScheduled = await _charges.GetMaxScheduledDateAsync(
            connection,
            transaction,
            subscription.Id,
            cancellationToken).ConfigureAwait(false);

        return lastScheduled is null
            ? subscription.StartDate
            : ChargeScheduling.NextDateAfter(versions, lastScheduled.Value);
    }
}
