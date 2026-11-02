using SubTrack.Core.Contracts;
using SubTrack.Core.Domain.Entities;

namespace SubTrack.Core.Services;

/// <summary>
/// F0-11, F0-T12 — Insumos del Dashboard reunidos por Infrastructure.
/// Los importes del mes vienen filtrados para el periodo consultado.
/// Los importes acumulados permiten calcular el saldo histórico global sin sesgar el mes.
/// </summary>
public sealed record DashboardInputs(
    long OpeningBalanceCents,
    long ConfirmedIncomeCents,
    long ConfirmedExpensesCents,
    long ConfirmedPaymentsCents,
    long PlannedIncomeCents,
    long PendingChargesCents,
    long PlannedExpensesCents,
    long? CumulativeIncomeCents = null,
    long? CumulativeExpensesCents = null,
    long? CumulativePaymentsCents = null);

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

        var cumulativeIncome = inputs.CumulativeIncomeCents ?? inputs.ConfirmedIncomeCents;
        var cumulativeExpenses = inputs.CumulativeExpensesCents.HasValue && inputs.CumulativePaymentsCents.HasValue
            ? checked(inputs.CumulativeExpensesCents.Value + inputs.CumulativePaymentsCents.Value)
            : totalExpenses;

        var currentBalance = checked(
            inputs.OpeningBalanceCents + cumulativeIncome - cumulativeExpenses);

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
