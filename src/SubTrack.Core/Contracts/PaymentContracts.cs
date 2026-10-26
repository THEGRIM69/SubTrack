namespace SubTrack.Core.Contracts;

/// <summary>
/// F0-T04 — Solicitud de registro de pago.
/// No lleva importe: el pago es siempre por el importe exacto del cobro (F0-04).
/// </summary>
public sealed record RegisterPaymentRequest(
    Guid ScheduledChargeId,
    DateOnly PaymentDate,
    Guid OperationId);

/// <summary>
/// F0-02, F0-T02 — Anulación de un pago conservando su historial.
/// </summary>
public sealed record CancelPaymentRequest(
    Guid PaymentId,
    string Reason);
