namespace SubTrack.Core.Domain.Enums;

/// <summary>
/// F0-22 — Indica si el importe de una suscripción es fijo o variable.
/// En las variables, el importe de la suscripción es solo una estimación
/// y el cobro se ajusta antes de registrarse el pago.
/// </summary>
public enum AmountType
{
    Fixed = 1,
    Variable = 2
}
