using SubTrack.Core.Abstractions;
using SubTrack.Infrastructure.Persistence;
using SubTrack.Infrastructure.Services;

namespace SubTrack.Infrastructure;

/// <summary>
/// Punto de composición de la capa de datos. WinForms solo ve interfaces de Core
/// y nunca tipos de SQLite (F0-T03).
/// </summary>
public sealed class SubTrackServices
{
    public SubTrackServices(SubTrackDatabaseOptions options, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        var factory = new SqliteConnectionFactory(options);
        var clock = new SystemClock(timeProvider);

        Subscriptions = new SubscriptionService(factory, clock);
        Charges = new ScheduledChargeService(factory, clock);
        Payments = new PaymentService(factory, clock);
        Expenses = new ExpenseService(factory, clock);
        Incomes = new IncomeService(factory, clock);
        Categories = new ExpenseCategoryService(factory, clock);
        OpeningBalance = new OpeningBalanceService(factory, clock);
        Dashboard = new DashboardService(factory, clock);
    }

    public ISubscriptionService Subscriptions { get; }

    public IScheduledChargeService Charges { get; }

    public IPaymentService Payments { get; }

    public IExpenseService Expenses { get; }

    public IIncomeService Incomes { get; }

    public IExpenseCategoryService Categories { get; }

    public IOpeningBalanceService OpeningBalance { get; }

    public IDashboardService Dashboard { get; }
}
