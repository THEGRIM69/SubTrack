namespace SubTrack.Core.Domain.Enums;

/// <summary>
/// F0-07, F0-19, F0-T08 — Estado de un gasto independiente.
/// Un gasto planificado no reduce el saldo real hasta confirmarse.
/// </summary>
public enum ExpenseStatus
{
    Planned = 1,
    Confirmed = 2,
    Cancelled = 3
}
