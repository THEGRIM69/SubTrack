using SubTrack.Core.Abstractions;
using SubTrack.Core.Contracts;
using SubTrack.Core.Results;
using SubTrack.Core.Services;
using SubTrack.Infrastructure.Persistence;
using SubTrack.Infrastructure.Persistence.Repositories;

namespace SubTrack.Infrastructure.Services;

/// <summary>
/// F0-11, F0-T12 — Reúne los insumos del Dashboard y delega el cálculo en Core.
/// Separa importes mensuales (para tarjetas y gráficos) de importes acumulados (para saldo global).
/// </summary>
internal sealed class DashboardService : IDashboardService
{
    private readonly SqliteConnectionFactory _factory;
    private readonly SystemClock _clock;
    private readonly DashboardRepository _dashboard = new();
    private readonly ScheduledChargeRepository _charges = new();
    private readonly OpeningBalanceRepository _balances = new();

    public DashboardService(SqliteConnectionFactory factory, SystemClock clock)
    {
        _factory = factory;
        _clock = clock;
    }

    public async Task<Result<DashboardSnapshot>> GetAsync(
        GetDashboardRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Month == default)
        {
            return Result<DashboardSnapshot>.Fail(
                ErrorCodes.FechaRequerida,
                "Debe indicar el mes a consultar.");
        }

        var today = _clock.Today;
        var first = new DateOnly(request.Month.Year, request.Month.Month, 1);
        var last = first.AddMonths(1).AddDays(-1);

        // F0-11: los movimientos previstos solo se proyectan desde hoy en adelante
        // para no contar dos veces lo que ya ocurrió.
        var futureFrom = today > first ? today : first;

        await using var connection = await _factory.OpenAsync(cancellationToken)
            .ConfigureAwait(false);

        var opening = await _balances.GetAsync(connection, null, cancellationToken)
            .ConfigureAwait(false);

        // F0-20, F0-21: el saldo inicial solo entra en vigor a partir de su fecha de inicio
        var openingAmount = (opening is not null && last >= opening.StartDate)
            ? opening.AmountCents
            : 0L;

        var monthlyIncome = await _dashboard.SumConfirmedIncomeRangeAsync(
            connection, first, last, cancellationToken).ConfigureAwait(false);
        var monthlyExpenses = await _dashboard.SumConfirmedExpensesRangeAsync(
            connection, first, last, cancellationToken).ConfigureAwait(false);
        var monthlyPayments = await _dashboard.SumConfirmedPaymentsRangeAsync(
            connection, first, last, cancellationToken).ConfigureAwait(false);

        var cumulativeIncome = await _dashboard.SumConfirmedIncomeAsync(
            connection, last, cancellationToken).ConfigureAwait(false);
        var cumulativeExpenses = await _dashboard.SumConfirmedExpensesAsync(
            connection, last, cancellationToken).ConfigureAwait(false);
        var cumulativePayments = await _dashboard.SumConfirmedPaymentsAsync(
            connection, last, cancellationToken).ConfigureAwait(false);

        var inputs = new DashboardInputs(
            OpeningBalanceCents: openingAmount,
            ConfirmedIncomeCents: monthlyIncome,
            ConfirmedExpensesCents: monthlyExpenses,
            ConfirmedPaymentsCents: monthlyPayments,
            PlannedIncomeCents: await _dashboard.SumPlannedIncomeAsync(
                connection, futureFrom, last, cancellationToken).ConfigureAwait(false),
            PendingChargesCents: await _dashboard.SumPendingChargesAsync(
                connection, futureFrom, last, cancellationToken).ConfigureAwait(false),
            PlannedExpensesCents: await _dashboard.SumPlannedExpensesAsync(
                connection, futureFrom, last, cancellationToken).ConfigureAwait(false),
            CumulativeIncomeCents: cumulativeIncome,
            CumulativeExpensesCents: cumulativeExpenses,
            CumulativePaymentsCents: cumulativePayments);

        var upcoming = futureFrom <= last
            ? await _charges.GetPendingInRangeAsync(connection, futureFrom, last, cancellationToken)
                .ConfigureAwait(false)
            : Array.Empty<SubTrack.Core.Domain.Entities.ScheduledCharge>();

        var overdue = await _charges.GetOverdueAsync(connection, today, cancellationToken)
            .ConfigureAwait(false);

        var activeSubscriptions = await _dashboard.CountActiveSubscriptionsAsync(
            connection, cancellationToken).ConfigureAwait(false);

        return Result<DashboardSnapshot>.Ok(DashboardCalculator.Calculate(
            first,
            inputs,
            activeSubscriptions,
            upcoming,
            overdue));
    }
}
