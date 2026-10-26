using SubTrack.Core.Domain.Enums;
using SubTrack.Core.Services;

namespace SubTrack.Tests.Core;

/// <summary>
/// F0-01, F0-10, F0-28 — Planificación de cobros sin duplicados.
/// </summary>
public sealed class ChargeSchedulePlannerTests
{
    [Fact]
    public void Plan_GeneraFechasAscendentesSinDuplicados()
    {
        var fechas = ChargeSchedulePlanner.Plan(
            from: new DateOnly(2026, 10, 25),
            until: new DateOnly(2027, 1, 31),
            startDate: new DateOnly(2026, 10, 28),
            deactivatedOn: null,
            frequency: BillingFrequency.Monthly,
            anchorDay: 28);

        Assert.Equal(
            new[]
            {
                new DateOnly(2026, 10, 28),
                new DateOnly(2026, 11, 28),
                new DateOnly(2026, 12, 28),
                new DateOnly(2027, 1, 28)
            },
            fechas);

        Assert.Equal(fechas.Count, fechas.Distinct().Count());
        Assert.Equal(fechas.OrderBy(f => f), fechas);
    }

    [Fact]
    public void Plan_RespetaLaFechaDeDesactivacion()
    {
        var fechas = ChargeSchedulePlanner.Plan(
            from: new DateOnly(2026, 10, 25),
            until: new DateOnly(2027, 6, 30),
            startDate: new DateOnly(2026, 10, 28),
            deactivatedOn: new DateOnly(2026, 12, 1),
            frequency: BillingFrequency.Monthly,
            anchorDay: 28);

        Assert.Equal(
            new[]
            {
                new DateOnly(2026, 10, 28),
                new DateOnly(2026, 11, 28)
            },
            fechas);
    }

    [Fact]
    public void Plan_SinRangoValido_DevuelveVacio()
    {
        var fechas = ChargeSchedulePlanner.Plan(
            from: new DateOnly(2027, 1, 1),
            until: new DateOnly(2026, 12, 31),
            startDate: new DateOnly(2026, 10, 28),
            deactivatedOn: null,
            frequency: BillingFrequency.Monthly,
            anchorDay: 28);

        Assert.Empty(fechas);
    }

    [Fact]
    public void Plan_ComienzaEnElInicioCuandoEsPosteriorAHoy()
    {
        var fechas = ChargeSchedulePlanner.Plan(
            from: new DateOnly(2026, 10, 25),
            until: new DateOnly(2026, 12, 31),
            startDate: new DateOnly(2026, 11, 15),
            deactivatedOn: null,
            frequency: BillingFrequency.Monthly,
            anchorDay: 15);

        Assert.Equal(
            new[]
            {
                new DateOnly(2026, 11, 15),
                new DateOnly(2026, 12, 15)
            },
            fechas);
    }

    [Fact]
    public void Plan_Diario_RespetaElDiaAnclaSinAlterarLaSecuencia()
    {
        var fechas = ChargeSchedulePlanner.Plan(
            from: new DateOnly(2026, 10, 25),
            until: new DateOnly(2026, 10, 28),
            startDate: new DateOnly(2026, 10, 25),
            deactivatedOn: null,
            frequency: BillingFrequency.Daily,
            anchorDay: 25);

        Assert.Equal(new[] { 25, 26, 27, 28 }, fechas.Select(f => f.Day));
    }
}
