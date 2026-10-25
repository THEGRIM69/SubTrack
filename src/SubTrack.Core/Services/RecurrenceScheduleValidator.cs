using SubTrack.Core.Domain.Enums;

namespace SubTrack.Core.Services;

/// <summary>
/// Comprueba si una fecha es exactamente el siguiente vencimiento esperado.
/// Reutiliza <see cref="NextChargeDateCalculator"/> para no duplicar la regla.
/// </summary>
public static class RecurrenceScheduleValidator
{
    public static bool IsExpectedNextDate(
        DateOnly previousScheduledDate,
        DateOnly candidateDate,
        BillingFrequency frequency,
        int anchorDay)
    {
        var expected = NextChargeDateCalculator.CalculateNextDate(
            previousScheduledDate,
            frequency,
            anchorDay);

        return expected == candidateDate;
    }
}
