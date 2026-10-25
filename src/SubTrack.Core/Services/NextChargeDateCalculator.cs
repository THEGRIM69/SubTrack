using SubTrack.Core.Domain.Enums;

namespace SubTrack.Core.Services;

/// <summary>
/// F0-T31 — Calcula el siguiente vencimiento conservando el día original.
/// Si el mes destino no tiene ese día, usa el último disponible sin mover
/// permanentemente la programación: 31/01 → 28/02 → 31/03.
/// </summary>
public static class NextChargeDateCalculator
{
    /// <summary>
    /// Calcula la fecha de cobro siguiente a <paramref name="lastScheduledDate"/>.
    /// </summary>
    /// <param name="lastScheduledDate">Última fecha programada.</param>
    /// <param name="frequency">Frecuencia de cobro.</param>
    /// <param name="anchorDay">Día original de vencimiento, entre 1 y 31.</param>
    public static DateOnly CalculateNextDate(
        DateOnly lastScheduledDate,
        BillingFrequency frequency,
        int anchorDay)
    {
        if (anchorDay is < 1 or > 31)
        {
            throw new ArgumentOutOfRangeException(
                nameof(anchorDay),
                anchorDay,
                "El día ancla debe estar entre 1 y 31.");
        }

        var months = frequency switch
        {
            BillingFrequency.Monthly => 1,
            BillingFrequency.Quarterly => 3,
            BillingFrequency.SemiAnnual => 6,
            BillingFrequency.Annual => 12,
            _ => 0
        };

        if (months == 0)
        {
            return frequency switch
            {
                BillingFrequency.Daily => lastScheduledDate.AddDays(1),
                BillingFrequency.Weekly => lastScheduledDate.AddDays(7),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(frequency),
                    frequency,
                    "Frecuencia de cobro no soportada.")
            };
        }

        var firstOfTargetMonth = new DateOnly(
            lastScheduledDate.Year,
            lastScheduledDate.Month,
            1).AddMonths(months);

        var day = Math.Min(
            anchorDay,
            DateTime.DaysInMonth(firstOfTargetMonth.Year, firstOfTargetMonth.Month));

        return new DateOnly(firstOfTargetMonth.Year, firstOfTargetMonth.Month, day);
    }
}
