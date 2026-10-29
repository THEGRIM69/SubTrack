using Microsoft.Data.Sqlite;
using SubTrack.Core.Abstractions;
using SubTrack.Core.Contracts;
using SubTrack.Core.Domain.Entities;
using SubTrack.Core.Domain.Enums;
using SubTrack.Core.Results;
using SubTrack.Core.Services;
using SubTrack.Infrastructure.Persistence;
using SubTrack.Infrastructure.Persistence.Repositories;

namespace SubTrack.Infrastructure.Services;

/// <summary>
/// F0-T03 — Implementación del alta, modificación y desactivación de suscripciones.
/// Toda escritura ocurre dentro de una transacción IMMEDIATE.
/// </summary>
internal sealed class SubscriptionService : ISubscriptionService
{
    private const int MaxHorizonMonths = 12;

    private readonly SqliteConnectionFactory _factory;
    private readonly SystemClock _clock;
    private readonly SubscriptionRepository _subscriptions = new();
    private readonly ScheduledChargeRepository _charges = new();

    public SubscriptionService(SqliteConnectionFactory factory, SystemClock clock)
    {
        _factory = factory;
        _clock = clock;
    }

    public async Task<Result<Subscription>> CreateAsync(
        CreateSubscriptionRequest request,
        CancellationToken cancellationToken = default)
    {
        var errors = ValidateCreate(request);

        if (errors.Count > 0)
        {
            return Result<Subscription>.Fail(errors);
        }

        var today = _clock.Today;
        var now = _clock.UtcNow;
        var anchorDay = request.StartDate.Day;
        var initialCharges = request.InitialPendingCharges ?? Array.Empty<InitialPendingCharge>();

        var nextChargeDate = initialCharges.Count > 0
            ? initialCharges.Min(charge => charge.ScheduledDate)
            : FirstScheduledDate(request.StartDate, request.Frequency, anchorDay, today);

        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            OperationId = request.OperationId,
            AmountCents = request.AmountCents,
            AmountType = request.AmountType,
            Frequency = request.Frequency,
            StartDate = request.StartDate,
            AnchorDay = anchorDay,
            NextChargeDate = nextChargeDate,
            IsActive = true,
            DeactivatedOn = null,
            CreatedAt = now,
            UpdatedAt = now
        };

        var version = new SubscriptionVersion
        {
            Id = Guid.NewGuid(),
            SubscriptionId = subscription.Id,
            VersionNumber = 1,
            EffectiveFrom = request.StartDate,
            AmountCents = request.AmountCents,
            AmountType = request.AmountType,
            Frequency = request.Frequency,
            AnchorDay = anchorDay,
            CreatedAt = now
        };

        await using var connection = await _factory.OpenAsync(cancellationToken)
            .ConfigureAwait(false);

        await using var transaction = connection.BeginTransaction(deferred: false);

        try
        {
            var existing = await _subscriptions.GetByOperationIdAsync(
                connection,
                transaction,
                request.OperationId,
                cancellationToken).ConfigureAwait(false);

            if (existing is not null)
            {
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return Result<Subscription>.Ok(existing);
            }

            await _subscriptions.InsertAsync(connection, transaction, subscription, cancellationToken)
                .ConfigureAwait(false);

            await _subscriptions.InsertVersionAsync(connection, transaction, version, cancellationToken)
                .ConfigureAwait(false);

            foreach (var initial in initialCharges)
            {
                await _charges.InsertAsync(
                    connection,
                    transaction,
                    new ScheduledCharge
                    {
                        Id = Guid.NewGuid(),
                        SubscriptionId = subscription.Id,
                        ScheduledDate = initial.ScheduledDate,
                        AmountCents = initial.AmountCents,
                        Status = ScheduledChargeStatus.Pending,
                        CreatedAt = now,
                        UpdatedAt = now
                    },
                    cancellationToken).ConfigureAwait(false);
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return Result<Subscription>.Ok(subscription);
        }
        catch (SqliteException exception) when (SqliteErrorTranslator.IsUniqueViolation(exception))
        {
            await ServiceSupport.SafeRollbackAsync(transaction, cancellationToken)
                .ConfigureAwait(false);

            var existing = await FindByOperationIdAsync(request.OperationId, cancellationToken)
                .ConfigureAwait(false);

            return existing is not null
                ? Result<Subscription>.Ok(existing)
                : Result<Subscription>.Fail(ErrorCodes.CobroDuplicado,
                    "Ya existe una suscripción o un cobro con esa operación.");
        }
        catch (SqliteException exception)
        {
            await ServiceSupport.SafeRollbackAsync(transaction, cancellationToken)
                .ConfigureAwait(false);

            return Result<Subscription>.Fail(SqliteErrorTranslator.Translate(exception));
        }
    }

    public async Task<Result<Subscription>> UpdateAsync(
        UpdateSubscriptionRequest request,
        CancellationToken cancellationToken = default)
    {
        var errors = ValidateUpdate(request);

        if (errors.Count > 0)
        {
            return Result<Subscription>.Fail(errors);
        }

        var today = _clock.Today;
        var now = _clock.UtcNow;

        if (request.EffectiveFrom < today)
        {
            return Result<Subscription>.Fail(
                ErrorCodes.FechaEfectivaAnterior,
                "La fecha de entrada en vigor no puede ser anterior a hoy.");
        }

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
                return Result<Subscription>.Fail(
                    ErrorCodes.SuscripcionNoEncontrada,
                    "La suscripción ya no existe.");
            }

            if (!subscription.IsActive)
            {
                return Result<Subscription>.Fail(
                    ErrorCodes.SuscripcionInactiva,
                    "La suscripción está desactivada y no admite cambios.");
            }

            var versions = await _subscriptions.GetVersionsAsync(
                connection,
                transaction,
                subscription.Id,
                cancellationToken).ConfigureAwait(false);

            var lastVersion = versions.Count > 0 ? versions[^1] : null;

            if (lastVersion is not null && request.EffectiveFrom <= lastVersion.EffectiveFrom)
            {
                return Result<Subscription>.Fail(
                    ErrorCodes.FechaEfectivaAnterior,
                    "Ya existe una versión vigente desde esa fecha o una posterior.");
            }

            var lastPaidCharge = await _charges.GetLastPaidWithActivePaymentAsync(
                connection,
                transaction,
                subscription.Id,
                cancellationToken).ConfigureAwait(false);

            if (lastPaidCharge is not null)
            {
                var coverageEnd = ChargeScheduling.NextDateAfter(versions, lastPaidCharge.ScheduledDate);
                if (request.EffectiveFrom < coverageEnd)
                {
                    return Result<Subscription>.Fail(
                        ErrorCodes.PeriodoCubiertoPorPago,
                        $"No se puede cambiar la suscripción antes del vencimiento del período ya pagado ({coverageEnd:yyyy-MM-dd}).");
                }
            }

            var horizon = await _charges.GetMaxScheduledDateAsync(
                connection,
                transaction,
                subscription.Id,
                cancellationToken).ConfigureAwait(false);

            if (horizon is null || horizon < request.EffectiveFrom)
            {
                horizon = request.EffectiveFrom.AddMonths(MaxHorizonMonths);
            }

            var newVersion = new SubscriptionVersion
            {
                Id = Guid.NewGuid(),
                SubscriptionId = subscription.Id,
                VersionNumber = (lastVersion?.VersionNumber ?? 0) + 1,
                EffectiveFrom = request.EffectiveFrom,
                AmountCents = request.AmountCents,
                AmountType = request.AmountType,
                Frequency = request.Frequency,
                AnchorDay = lastVersion?.AnchorDay ?? subscription.AnchorDay,
                CreatedAt = now
            };

            await _subscriptions.InsertVersionAsync(
                connection,
                transaction,
                newVersion,
                cancellationToken).ConfigureAwait(false);

            var updated = subscription with
            {
                Name = request.Name.Trim(),
                AmountCents = request.AmountCents,
                AmountType = request.AmountType,
                Frequency = request.Frequency,
                AnchorDay = newVersion.AnchorDay,
                UpdatedAt = now
            };

            await _subscriptions.UpdateAsync(connection, transaction, updated, cancellationToken)
                .ConfigureAwait(false);

            await _charges.DeleteFuturePendingWithoutPaymentsAsync(
                connection,
                transaction,
                subscription.Id,
                request.EffectiveFrom,
                cancellationToken).ConfigureAwait(false);

            var allVersions = versions.Append(newVersion).ToList();

            foreach (var plan in ChargeScheduling.PlanSegments(
                allVersions,
                request.EffectiveFrom,
                horizon.Value,
                null))
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

                await _charges.InsertAsync(
                    connection,
                    transaction,
                    NewCharge(subscription.Id, plan, now),
                    cancellationToken).ConfigureAwait(false);
            }

            var next = await _charges.GetMinPendingDateAsync(
                connection,
                transaction,
                subscription.Id,
                cancellationToken).ConfigureAwait(false) ?? request.EffectiveFrom;

            await _subscriptions.UpdateNextChargeAsync(
                connection,
                transaction,
                subscription.Id,
                next,
                now,
                cancellationToken).ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return Result<Subscription>.Ok(updated with { NextChargeDate = next });
        }
        catch (SqliteException exception)
        {
            await ServiceSupport.SafeRollbackAsync(transaction, cancellationToken)
                .ConfigureAwait(false);

            return Result<Subscription>.Fail(SqliteErrorTranslator.Translate(exception));
        }
    }

    public async Task<Result<Subscription>> DeactivateAsync(
        DeactivateSubscriptionRequest request,
        CancellationToken cancellationToken = default)
    {
        var today = _clock.Today;
        var now = _clock.UtcNow;

        if (request.EffectiveFrom < today)
        {
            return Result<Subscription>.Fail(
                ErrorCodes.FechaEfectivaAnterior,
                "La fecha de desactivación no puede ser anterior a hoy.");
        }

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
                return Result<Subscription>.Fail(
                    ErrorCodes.SuscripcionNoEncontrada,
                    "La suscripción ya no existe.");
            }

            if (!subscription.IsActive)
            {
                return Result<Subscription>.Fail(
                    ErrorCodes.SuscripcionYaInactiva,
                    "La suscripción ya estaba desactivada.");
            }

            await _charges.DeleteFuturePendingWithoutPaymentsAsync(
                connection,
                transaction,
                subscription.Id,
                request.EffectiveFrom,
                cancellationToken).ConfigureAwait(false);

            var next = await _charges.GetMinPendingDateAsync(
                connection,
                transaction,
                subscription.Id,
                cancellationToken).ConfigureAwait(false);

            await _subscriptions.DeactivateAsync(
                connection,
                transaction,
                subscription.Id,
                request.EffectiveFrom,
                next,
                now,
                cancellationToken).ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return Result<Subscription>.Ok(subscription with
            {
                IsActive = false,
                DeactivatedOn = request.EffectiveFrom,
                NextChargeDate = next,
                UpdatedAt = now
            });
        }
        catch (SqliteException exception)
        {
            await ServiceSupport.SafeRollbackAsync(transaction, cancellationToken)
                .ConfigureAwait(false);

            return Result<Subscription>.Fail(SqliteErrorTranslator.Translate(exception));
        }
    }

    public async Task<Result<IReadOnlyList<Subscription>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _factory.OpenAsync(cancellationToken)
            .ConfigureAwait(false);

        var subscriptions = await _subscriptions.GetAllAsync(connection, cancellationToken)
            .ConfigureAwait(false);

        return Result<IReadOnlyList<Subscription>>.Ok(subscriptions);
    }

    public async Task<Result<Subscription>> GetByIdAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _factory.OpenAsync(cancellationToken)
            .ConfigureAwait(false);

        var subscription = await _subscriptions.GetByIdAsync(
            connection,
            null,
            subscriptionId,
            cancellationToken).ConfigureAwait(false);

        return subscription is null
            ? Result<Subscription>.Fail(
                ErrorCodes.SuscripcionNoEncontrada,
                "La suscripción no existe.")
            : Result<Subscription>.Ok(subscription);
    }

    public async Task<Result<IReadOnlyList<SubscriptionVersion>>> GetVersionsAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _factory.OpenAsync(cancellationToken)
            .ConfigureAwait(false);

        var subscription = await _subscriptions.GetByIdAsync(
            connection,
            null,
            subscriptionId,
            cancellationToken).ConfigureAwait(false);

        if (subscription is null)
        {
            return Result<IReadOnlyList<SubscriptionVersion>>.Fail(
                ErrorCodes.SuscripcionNoEncontrada,
                "La suscripción no existe.");
        }

        var versions = await _subscriptions.GetVersionsAsync(
            connection,
            null,
            subscriptionId,
            cancellationToken).ConfigureAwait(false);

        return Result<IReadOnlyList<SubscriptionVersion>>.Ok(versions);
    }

    private static ScheduledCharge NewCharge(
        Guid subscriptionId,
        PlannedCharge plan,
        DateTimeOffset now) => new()
    {
        Id = Guid.NewGuid(),
        SubscriptionId = subscriptionId,
        ScheduledDate = plan.Date,
        AmountCents = plan.Version.AmountCents,
        Status = ScheduledChargeStatus.Pending,
        CreatedAt = now,
        UpdatedAt = now
    };

    private static DateOnly FirstScheduledDate(
        DateOnly startDate,
        BillingFrequency frequency,
        int anchorDay,
        DateOnly today)
    {
        if (startDate >= today)
        {
            return startDate;
        }

        var scheduled = ChargeSchedulePlanner.Plan(
            today,
            today.AddYears(2),
            startDate,
            null,
            frequency,
            anchorDay);

        return scheduled.Count > 0 ? scheduled[0] : startDate;
    }

    private async Task<Subscription?> FindByOperationIdAsync(
        Guid operationId,
        CancellationToken cancellationToken)
    {
        await using var connection = await _factory.OpenAsync(cancellationToken)
            .ConfigureAwait(false);

        return await _subscriptions.GetByOperationIdAsync(
            connection,
            null,
            operationId,
            cancellationToken).ConfigureAwait(false);
    }

    private static List<DomainError> ValidateCreate(CreateSubscriptionRequest request)
    {
        var errors = new List<DomainError>();

        if (DomainValidation.Name(request.Name) is { } nameError)
        {
            errors.Add(nameError);
        }

        if (DomainValidation.Amount(request.AmountCents) is { } amountError)
        {
            errors.Add(amountError);
        }

        if (!Enum.IsDefined(request.AmountType))
        {
            errors.Add(new DomainError(
                ErrorCodes.EstadoInicialInvalido,
                "El tipo de importe no es válido.",
                "AmountType"));
        }

        if (!Enum.IsDefined(request.Frequency))
        {
            errors.Add(new DomainError(
                ErrorCodes.EstadoInicialInvalido,
                "La frecuencia de cobro no es válida.",
                "Frequency"));
        }

        if (request.StartDate == default)
        {
            errors.Add(new DomainError(
                ErrorCodes.FechaRequerida,
                "Debe indicar la fecha de inicio.",
                "StartDate"));
        }

        if (request.OperationId == Guid.Empty)
        {
            errors.Add(new DomainError(
                ErrorCodes.OperacionInvalida,
                "La operación no es válida. Vuelva a intentarlo.",
                "OperationId"));
        }

        var initial = request.InitialPendingCharges ?? Array.Empty<InitialPendingCharge>();

        foreach (var charge in initial)
        {
            if (charge.AmountCents <= 0)
            {
                errors.Add(new DomainError(
                    ErrorCodes.ImporteInvalido,
                    "El importe de los cobros anteriores debe ser mayor que cero.",
                    "InitialPendingCharges"));
            }
        }

        if (initial.Select(charge => charge.ScheduledDate).Distinct().Count() != initial.Count)
        {
            errors.Add(new DomainError(
                ErrorCodes.CobroDuplicado,
                "Hay dos cobros anteriores con la misma fecha.",
                "InitialPendingCharges"));
        }

        return errors;
    }

    private static List<DomainError> ValidateUpdate(UpdateSubscriptionRequest request)
    {
        var errors = new List<DomainError>();

        if (DomainValidation.Name(request.Name) is { } nameError)
        {
            errors.Add(nameError);
        }

        if (DomainValidation.Amount(request.AmountCents) is { } amountError)
        {
            errors.Add(amountError);
        }

        if (!Enum.IsDefined(request.AmountType))
        {
            errors.Add(new DomainError(
                ErrorCodes.EstadoInicialInvalido,
                "El tipo de importe no es válido.",
                "AmountType"));
        }

        if (!Enum.IsDefined(request.Frequency))
        {
            errors.Add(new DomainError(
                ErrorCodes.EstadoInicialInvalido,
                "La frecuencia de cobro no es válida.",
                "Frequency"));
        }

        if (request.EffectiveFrom == default)
        {
            errors.Add(new DomainError(
                ErrorCodes.FechaRequerida,
                "Debe indicar la fecha de entrada en vigor.",
                "EffectiveFrom"));
        }

        return errors;
    }
}
