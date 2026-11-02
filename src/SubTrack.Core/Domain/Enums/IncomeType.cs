namespace SubTrack.Core.Domain.Enums;

/// <summary>
/// F0-06, F0-10, F0-T07 — Tipo de ingreso.
/// <see cref="Biweekly"/> se recibe los días 15 y último del mes.
/// <see cref="Extraordinary"/> es ocasional y nunca proviene de una plantilla.
/// </summary>
public enum IncomeType
{
    Monthly = 1,
    Biweekly = 2,
    Extraordinary = 3
}
