namespace SubTrack.Core.Domain.Enums;

/// <summary>
/// F0-06, F0-19 — Estado de un ingreso.
/// Un ingreso previsto no cuenta como dinero recibido hasta confirmarse.
/// </summary>
public enum IncomeStatus
{
    Planned = 1,
    Confirmed = 2,
    Cancelled = 3
}
