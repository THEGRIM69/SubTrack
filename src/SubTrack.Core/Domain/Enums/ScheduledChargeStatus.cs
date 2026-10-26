namespace SubTrack.Core.Domain.Enums;

/// <summary>
/// F0-T01 — Estado de un cobro programado.
/// Un cobro pagado puede volver a pendiente si su pago se anula (F0-02).
/// </summary>
public enum ScheduledChargeStatus
{
    Pending = 1,
    Paid = 2,
    Cancelled = 3
}
