using SubTrack.Core.Services;

namespace SubTrack.Tests.Core;

/// <summary>
/// F0-11, F0-T12 — Fórmulas del Dashboard centralizadas en Core.
/// </summary>
public sealed class DashboardCalculatorTests
{
    private static readonly DateOnly Mes = new(2026, 10, 1);

    [Fact]
    public void Calculate_ReproduceElEjemploDeLaDecision()
    {
        var snapshot = DashboardCalculator.Calculate(
            Mes,
            new DashboardInputs(
                OpeningBalanceCents: 50_000,
                ConfirmedIncomeCents: 30_000,
                ConfirmedExpensesCents: 4_000,
                ConfirmedPaymentsCents: 0,
                PlannedIncomeCents: 0,
                PendingChargesCents: 0,
                PlannedExpensesCents: 0),
            activeSubscriptions: 3,
            upcomingCharges: Array.Empty<SubTrack.Core.Domain.Entities.ScheduledCharge>(),
            overdueCharges: Array.Empty<SubTrack.Core.Domain.Entities.ScheduledCharge>());

        Assert.Equal(4_000, snapshot.TotalExpensesCents);
        Assert.Equal(76_000, snapshot.CurrentBalanceCents);
        Assert.Equal(76_000, snapshot.ProjectedSavingsCents);
    }

    [Fact]
    public void Calculate_EgresosRealizadosSumanPagosYGastos()
    {
        var snapshot = DashboardCalculator.Calculate(
            Mes,
            new DashboardInputs(0, 100_000, 25_000, 15_000, 0, 0, 0),
            0,
            Array.Empty<SubTrack.Core.Domain.Entities.ScheduledCharge>(),
            Array.Empty<SubTrack.Core.Domain.Entities.ScheduledCharge>());

        Assert.Equal(15_000, snapshot.SubscriptionPaymentsCents);
        Assert.Equal(25_000, snapshot.ExpensesCents);
        Assert.Equal(40_000, snapshot.TotalExpensesCents);
        Assert.Equal(60_000, snapshot.CurrentBalanceCents);
    }

    [Fact]
    public void Calculate_AhorroProyectadoDescuentaObligacionesPendientes()
    {
        var snapshot = DashboardCalculator.Calculate(
            Mes,
            new DashboardInputs(
                OpeningBalanceCents: 100_000,
                ConfirmedIncomeCents: 0,
                ConfirmedExpensesCents: 0,
                ConfirmedPaymentsCents: 0,
                PlannedIncomeCents: 50_000,
                PendingChargesCents: 30_000,
                PlannedExpensesCents: 10_000),
            0,
            Array.Empty<SubTrack.Core.Domain.Entities.ScheduledCharge>(),
            Array.Empty<SubTrack.Core.Domain.Entities.ScheduledCharge>());

        Assert.Equal(110_000, snapshot.ProjectedSavingsCents);
    }

    [Fact]
    public void Calculate_AhorroNegativoRepresentaDeficit()
    {
        var snapshot = DashboardCalculator.Calculate(
            Mes,
            new DashboardInputs(0, 0, 0, 0, 0, 0, 25_000),
            0,
            Array.Empty<SubTrack.Core.Domain.Entities.ScheduledCharge>(),
            Array.Empty<SubTrack.Core.Domain.Entities.ScheduledCharge>());

        Assert.Equal(-25_000, snapshot.ProjectedSavingsCents);
        Assert.True(snapshot.ProjectedSavingsCents < 0);
    }

    [Fact]
    public void Calculate_DesbordamientoLanzaExcepcion()
    {
        Assert.Throws<OverflowException>(() => DashboardCalculator.Calculate(
            Mes,
            new DashboardInputs(long.MaxValue, 1, 0, 0, 0, 0, 0),
            0,
            Array.Empty<SubTrack.Core.Domain.Entities.ScheduledCharge>(),
            Array.Empty<SubTrack.Core.Domain.Entities.ScheduledCharge>()));
    }
}
