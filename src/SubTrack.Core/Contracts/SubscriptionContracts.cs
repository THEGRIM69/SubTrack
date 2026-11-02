using SubTrack.Core.Domain.Enums;

namespace SubTrack.Core.Contracts;

/// <summary>
/// F0-25 — Obligación anterior a la instalación que sigue pendiente de pago.
/// </summary>
public sealed record InitialPendingCharge(DateOnly ScheduledDate, long AmountCents);

/// <summary>
/// F0-27 — Alta de suscripción. <c>OperationId</c> evita duplicados por doble envío.
/// </summary>
public sealed record CreateSubscriptionRequest(
    string Name,
    long AmountCents,
    AmountType AmountType,
    BillingFrequency Frequency,
    DateOnly StartDate,
    Guid OperationId,
    IReadOnlyList<InitialPendingCharge>? InitialPendingCharges = null);

/// <summary>
/// F0-09, F0-23 — Modificación con entrada en vigor. Solo aplica hacia el futuro.
/// </summary>
public sealed record UpdateSubscriptionRequest(
    Guid SubscriptionId,
    string Name,
    long AmountCents,
    AmountType AmountType,
    BillingFrequency Frequency,
    DateOnly EffectiveFrom);

/// <summary>
/// F0-10 — Desactivación. Conserva los cobros y pagos anteriores.
/// </summary>
public sealed record DeactivateSubscriptionRequest(
    Guid SubscriptionId,
    DateOnly EffectiveFrom);
