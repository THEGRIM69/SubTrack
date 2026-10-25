using SubTrack.Core.Contracts;
using SubTrack.Core.Domain.Entities;

namespace SubTrack.Core.Services;

/// <summary>
/// F0-11, F0-T12 — Insumos del Dashboard reunidos por Infrastructure.
/// Los importes ya vienen filtrados: solo movimientos vigentes del mes solicitado.
/// </summary>
public sealed record DashboardInputs(
    long OpeningBalanceCents,
    long ConfirmedIncomeCents,
    long ConfirmedExpensesCents,
    long ConfirmedPaymentsCents,
    long PlannedIncomeCents,
    long PendingChargesCents,
    long PlannedExpensesCents);

/// <summary>
/// F0-11, F0-T12 — Cálculo financiero centralizado en Core.
/// WinForms nunca recalcula estos valores.
/// </summary>
public static class DashboardCalculator
{
    public static DashboardSnapshot Calculate(
        DateOnly month,
        DashboardInputs inputs,
        int activeSubscriptions,
        IReadOnlyList<ScheduledCharge> upcomingCharges,
        IReadOnlyList<ScheduledCharge> overdueCharges)
    {
        var totalExpenses = checked(
            inputs.ConfirmedPaymentsCents + inputs.ConfirmedExpensesCents);

        var currentBalance = checked(
            inputs.OpeningBalanceCents + inputs.ConfirmedIncomeCents - totalExpenses);

        var projectedSavings = checked(
            currentBalance
            + inputs.PlannedIncomeCents
            - inputs.PendingChargesCents
            - inputs.PlannedExpensesCents);

        return new DashboardSnapshot(
            month,
            activeSubscriptions,
            upcomingCharges,
            overdueCharges,
            inputs.ConfirmedPaymentsCents,
            inputs.ConfirmedExpensesCents,
            totalExpenses,
            inputs.ConfirmedIncomeCents,
            inputs.OpeningBalanceCents,
            currentBalance,
            projectedSavings);
    }
}
