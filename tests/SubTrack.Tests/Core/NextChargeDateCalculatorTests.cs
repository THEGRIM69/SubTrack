using SubTrack.Core.Domain.Enums;
using SubTrack.Core.Services;

namespace SubTrack.Tests.Core;

/// <summary>
/// F0-T31 — El vencimiento conserva el día original aunque el mes sea más corto.
/// </summary>
public sealed class NextChargeDateCalculatorTests
{
    [Fact]
    public void Mensual_MesCorto_VuelveAlDiaOriginal()
    {
        var fecha = new DateOnly(2027, 1, 31);

        fecha = NextChargeDateCalculator.CalculateNextDate(fecha, BillingFrequency.Monthly, 31);
        Assert.Equal(new DateOnly(2027, 2, 28), fecha);

        fecha = NextChargeDateCalculator.CalculateNextDate(fecha, BillingFrequency.Monthly, 31);
        Assert.Equal(new DateOnly(2027, 3, 31), fecha);

        fecha = NextChargeDateCalculator.CalculateNextDate(fecha, BillingFrequency.Monthly, 31);
        Assert.Equal(new DateOnly(2027, 4, 30), fecha);

        fecha = NextChargeDateCalculator.CalculateNextDate(fecha, BillingFrequency.Monthly, 31);
        Assert.Equal(new DateOnly(2027, 5, 31), fecha);
    }

    [Fact]
    public void Mensual_AnioBisiesto_UsaElVeintinueve()
    {
        var febrero = NextChargeDateCalculator.CalculateNextDate(
            new DateOnly(2028, 1, 31), BillingFrequency.Monthly, 31);

        Assert.Equal(new DateOnly(2028, 2, 29), febrero);

        var marzo = NextChargeDateCalculator.CalculateNextDate(
            febrero, BillingFrequency.Monthly, 31);

        Assert.Equal(new DateOnly(2028, 3, 31), marzo);
    }

    [Fact]
    public void Mensual_DiaNormal_NoSeAltera()
    {
        var siguiente = NextChargeDateCalculator.CalculateNextDate(
            new DateOnly(2026, 10, 15), BillingFrequency.Monthly, 15);

        Assert.Equal(new DateOnly(2026, 11, 15), siguiente);
    }

    [Theory]
    [InlineData(BillingFrequency.Daily, 2026, 10, 26)]
    [InlineData(BillingFrequency.Weekly, 2026, 11, 1)]
    [InlineData(BillingFrequency.Monthly, 2026, 11, 25)]
    [InlineData(BillingFrequency.Quarterly, 2027, 1, 25)]
    [InlineData(BillingFrequency.SemiAnnual, 2027, 4, 25)]
    [InlineData(BillingFrequency.Annual, 2027, 10, 25)]
    public void CadaFrecuencia_SumaElPeriodoCorrecto(
        BillingFrequency frecuencia,
        int anio,
        int mes,
        int dia)
    {
        var siguiente = NextChargeDateCalculator.CalculateNextDate(
            new DateOnly(2026, 10, 25), frecuencia, 25);

        Assert.Equal(new DateOnly(anio, mes, dia), siguiente);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    [InlineData(-1)]
    public void DiaAnclaFueraDeRango_LanzaExcepcion(int diaAncla)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            NextChargeDateCalculator.CalculateNextDate(
                new DateOnly(2026, 10, 25), BillingFrequency.Monthly, diaAncla));
    }

    [Fact]
    public void Validator_ReconoceElSiguienteVencimientoEsperado()
    {
        Assert.True(RecurrenceScheduleValidator.IsExpectedNextDate(
            new DateOnly(2027, 1, 31),
            new DateOnly(2027, 2, 28),
            BillingFrequency.Monthly,
            31));

        Assert.False(RecurrenceScheduleValidator.IsExpectedNextDate(
            new DateOnly(2027, 1, 31),
            new DateOnly(2027, 3, 28),
            BillingFrequency.Monthly,
            31));
    }
}
