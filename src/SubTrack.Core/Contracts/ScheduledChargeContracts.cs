namespace SubTrack.Core.Contracts;

/// <summary>
/// F0-01, F0-28 — Generación explícita de cobros hasta una fecha (máximo 12 meses).
/// </summary>
public sealed record GenerateChargesRequest(
    Guid SubscriptionId,
    DateOnly Until);

/// <summary>
/// F0-22 — Ajuste del importe estimado antes de registrar el pago (suscripciones variables).
/// </summary>
public sealed record AdjustChargeAmountRequest(
    Guid ScheduledChargeId,
    long AmountCents);

/// <summary>
/// F0-25 — Registro de una obligación anterior a la instalación.
/// </summary>
public sealed record RegisterInitialPendingChargeRequest(
    Guid SubscriptionId,
    DateOnly ScheduledDate,
    long AmountCents);
