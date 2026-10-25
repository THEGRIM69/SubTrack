using SubTrack.Core.Domain.Enums;

namespace SubTrack.Core.Services;

/// <summary>
/// F0-01, F0-28 — Planificador puro de cobros programados.
/// No consulta la base de datos: Infrastructure descarta después las fechas que ya existen.
/// </summary>
public static class ChargeSchedulePlanner
{
    private const int MaxIterations = 100_000;

    /// <summary>
    /// Devuelve las fechas que deben existir entre <paramref name="from"/> y
    /// <paramref name="until"/>, sin duplicados y en orden ascendente.
    /// </summary>
    /// <param name="from">Primera fecha admitida (normalmente hoy).</param>
    /// <param name="until">Última fecha admitida (horizonte solicitado).</param>
    /// <param name="startDate">Inicio de la programación de la suscripción.</param>
    /// <param name="deactivatedOn">Fecha efectiva de cancelación, si existe.</param>
    /// <param name="frequency">Frecuencia de cobro.</param>
    /// <param name="anchorDay">Día original de vencimiento.</param>
    public static IReadOnlyList<DateOnly> Plan(
        DateOnly from,
        DateOnly until,
        DateOnly startDate,
        DateOnly? deactivatedOn,
        BillingFrequency frequency,
        int anchorDay)
    {
        var limit = deactivatedOn is { } deactivated && deactivated < until
            ? deactivated
            : until;

        if (limit < from || limit < startDate)
        {
            return Array.Empty<DateOnly>();
        }

        var cursor = startDate;
        var iterations = 0;

        while (cursor < from)
        {
            cursor = NextChargeDateCalculator.CalculateNextDate(cursor, frequency, anchorDay);
            GuardIterations(++iterations);
        }

        var dates = new List<DateOnly>();

        while (cursor <= limit)
        {
            dates.Add(cursor);
            cursor = NextChargeDateCalculator.CalculateNextDate(cursor, frequency, anchorDay);
            GuardIterations(++iterations);
        }

        return dates;
    }

    private static void GuardIterations(int iterations)
    {
        if (iterations > MaxIterations)
        {
            throw new InvalidOperationException(
                "El rango de generación excede el máximo procesable. Reduzca el período solicitado.");
        }
    }
}
