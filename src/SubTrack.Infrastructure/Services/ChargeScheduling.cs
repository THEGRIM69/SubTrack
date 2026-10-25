using SubTrack.Core.Domain.Entities;
using SubTrack.Core.Services;

namespace SubTrack.Infrastructure.Services;

internal sealed record PlannedCharge(DateOnly Date, SubscriptionVersion Version);

/// <summary>
/// F0-T06 — Traduce el historial de versiones en fechas de cobro.
/// Cada versión rige desde su fecha de entrada en vigor, de modo que un cambio
/// con efecto futuro no altera los cobros anteriores.
/// </summary>
internal static class ChargeScheduling
{
    /// <summary>Versión vigente en una fecha concreta.</summary>
    public static SubscriptionVersion VersionAt(
        IReadOnlyList<SubscriptionVersion> versions,
        DateOnly date)
    {
        if (versions.Count == 0)
        {
            throw new InvalidOperationException(
                "La suscripción no tiene versiones registradas.");
        }

        return versions.LastOrDefault(version => version.EffectiveFrom <= date)
            ?? versions[0];
    }

    /// <summary>
    /// Fechas a generar entre <paramref name="from"/> y <paramref name="until"/>,
    /// respetando los cambios de versión y la fecha de desactivación.
    /// </summary>
    public static IReadOnlyList<PlannedCharge> PlanSegments(
        IReadOnlyList<SubscriptionVersion> versions,
        DateOnly from,
        DateOnly until,
        DateOnly? deactivatedOn)
    {
        if (versions.Count == 0 || until < from)
        {
            return Array.Empty<PlannedCharge>();
        }

        var planned = new List<PlannedCharge>();

        for (var index = 0; index < versions.Count; index++)
        {
            var version = versions[index];
            var start = version.EffectiveFrom > from ? version.EffectiveFrom : from;
            var end = until;

            if (index + 1 < versions.Count)
            {
                var nextEffective = versions[index + 1].EffectiveFrom.AddDays(-1);

                if (nextEffective < end)
                {
                    end = nextEffective;
                }
            }

            if (deactivatedOn is { } deactivated && deactivated < end)
            {
                end = deactivated;
            }

            if (start > end)
            {
                continue;
            }

            foreach (var date in ChargeSchedulePlanner.Plan(
                start,
                end,
                version.EffectiveFrom,
                null,
                version.Frequency,
                version.AnchorDay))
            {
                planned.Add(new PlannedCharge(date, version));
            }
        }

        return planned
            .GroupBy(plan => plan.Date)
            .Select(group => group.MaxBy(plan => plan.Version.EffectiveFrom)!)
            .OrderBy(plan => plan.Date)
            .ToList();
    }

    /// <summary>Siguiente fecha natural después de un cobro, según su versión.</summary>
    public static DateOnly NextDateAfter(
        IReadOnlyList<SubscriptionVersion> versions,
        DateOnly scheduledDate)
    {
        var version = VersionAt(versions, scheduledDate);

        return NextChargeDateCalculator.CalculateNextDate(
            scheduledDate,
            version.Frequency,
            version.AnchorDay);
    }
}
