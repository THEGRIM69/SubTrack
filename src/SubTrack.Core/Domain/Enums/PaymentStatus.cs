namespace SubTrack.Core.Domain.Enums;

/// <summary>
/// F0-02 — Estado de un pago.
/// Solo puede existir un pago <see cref="Active"/> por cobro; los anulados
/// se conservan como historial y no cuentan en los totales.
/// </summary>
public enum PaymentStatus
{
    Active = 1,
    Cancelled = 2
}
