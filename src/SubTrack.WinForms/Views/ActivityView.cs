using SubTrack.Core.Contracts;
using SubTrack.Core.Domain.Entities;
using SubTrack.Core.Domain.Enums;
using SubTrack.WinForms.Theme;
using SubTrack.WinForms.UI;

namespace SubTrack.WinForms.Views;

internal sealed class ActivityView : ViewBase
{
    private readonly MonthStepper _month = new();
    private readonly DataGridView _upcoming = Ui.Grid(("Fecha", 26f), ("Suscripción", 50f), ("Importe", 24f));
    private readonly DataGridView _activity = Ui.Grid(("Fecha", 22f), ("Actividad", 42f), ("Importe", 20f), ("Estado", 16f));
    private readonly Label _upcomingCount = Counter(Palette.PrimaryDeep);
    private readonly Label _activityCount = Counter(Palette.Muted);
    private readonly Panel _scrollHost = new() { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Palette.Canvas };
    private readonly TableLayoutPanel _content = new()
    {
        BackColor = Palette.Canvas,
        ColumnCount = 1,
        RowCount = 2,
        Margin = Padding.Empty,
        Padding = Padding.Empty
    };
    private Dictionary<Guid, string> _names = new();

    public ActivityView(ViewContext context)
        : base(context)
    {
    }

    public override string Title => "Actividad";

    public override string Subtitle => "Próximos pagos y movimientos recientes.";

    protected override void Build()
    {
        _month.MonthChanged += async () => await RefreshAsync();

        var caption = Ui.Caption("MES");
        caption.Margin = new Padding(4, 13, 10, 0);

        var root = new TableLayoutPanel
        {
            BackColor = Palette.Canvas,
            ColumnCount = 1,
            RowCount = 2,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.Controls.Add(
            Ui.Toolbar(
                new Control[] { caption, _month },
                new Control[] { Ui.Action("Actualizar", RefreshAsync) }),
            0,
            0);

        _upcoming.RowTemplate.Height = 38;
        _upcoming.ColumnHeadersHeight = 34;
        _upcoming.ScrollBars = ScrollBars.None;
        _activity.RowTemplate.Height = 38;
        _activity.ColumnHeadersHeight = 34;
        _activity.ScrollBars = ScrollBars.None;
        Ui.Align(_upcoming, "Importe");
        Ui.Align(_activity, "Importe");

        var upcomingCard = BuildList("Próximos pagos", _upcoming, _upcomingCount);
        var activityCard = BuildList("Actividad reciente", _activity, _activityCount);
        activityCard.Margin = new Padding(0, 0, 0, 14);

        _content.Controls.Add(upcomingCard, 0, 0);
        _content.Controls.Add(activityCard, 0, 1);
        _scrollHost.Controls.Add(_content);
        _scrollHost.SizeChanged += (_, _) => UpdateContentLayout();
        root.Controls.Add(_scrollHost, 0, 1);
        Controls.Add(root);
    }

    protected override async Task LoadAsync()
    {
        var dashboardTask = Context.Services.Dashboard.GetAsync(new GetDashboardRequest(_month.Month));
        var subscriptionsTask = Context.Services.Subscriptions.GetAllAsync();
        var paymentsTask = Context.Services.Payments.GetHistoryAsync(new PageRequest(1, 100));
        var expensesTask = Context.Services.Expenses.GetHistoryAsync(new ExpenseHistoryRequest(new PageRequest(1, 100)));
        var incomesTask = Context.Services.Incomes.GetHistoryAsync(new IncomeHistoryRequest(new PageRequest(1, 100)));

        await Task.WhenAll(dashboardTask, subscriptionsTask, paymentsTask, expensesTask, incomesTask);

        var dashboard = await dashboardTask;
        IReadOnlyList<ScheduledCharge> upcomingCharges = Array.Empty<ScheduledCharge>();
        if (Failed(dashboard))
        {
            _upcoming.Rows.Clear();
            _upcomingCount.Text = "No se pudo cargar";
        }
        else
        {
            upcomingCharges = dashboard.Value.UpcomingCharges;
        }

        var subscriptions = await subscriptionsTask;
        if (Failed(subscriptions))
        {
            _names.Clear();
        }
        else
        {
            _names = subscriptions.Value.ToDictionary(item => item.Id, item => item.Name);
        }

        if (dashboard.IsSuccess)
        {
            FillUpcoming(upcomingCharges);
        }

        var payments = await paymentsTask;
        var expenses = await expensesTask;
        var incomes = await incomesTask;
        if (payments.IsFailure || expenses.IsFailure || incomes.IsFailure)
        {
            if (payments.IsFailure) Failed(payments);
            if (expenses.IsFailure) Failed(expenses);
            if (incomes.IsFailure) Failed(incomes);
            _activity.Rows.Clear();
            _activityCount.Text = "No se pudo cargar";
            UpdateContentLayout();
            return;
        }

        FillActivity(payments.Value.Items, expenses.Value.Items, incomes.Value.Items);
        UpdateContentLayout();
    }

    private void FillUpcoming(IReadOnlyList<ScheduledCharge> charges)
    {
        _upcoming.Rows.Clear();
        _upcomingCount.Text = charges.Count == 0
            ? "Sin cargos pendientes próximos"
            : $"{charges.Count} próximos";

        foreach (var charge in charges)
        {
            var name = _names.TryGetValue(charge.SubscriptionId, out var value)
                ? value
                : "Suscripción no disponible";
            var index = _upcoming.Rows.Add(
                Format.Date(charge.ScheduledDate),
                name,
                Format.Money(charge.AmountCents));
            _upcoming.Rows[index].Tag = charge;
        }

        UpdateContentLayout();
    }

    private void FillActivity(
        IReadOnlyList<Payment> payments,
        IReadOnlyList<Expense> expenses,
        IReadOnlyList<Income> incomes)
    {
        var items = new List<ActivityItem>();

        foreach (var payment in payments)
        {
            var (status, color) = Ui.PaymentBadge(payment.Status);
            var activityDate = payment.Status == PaymentStatus.Cancelled && payment.CancelledAt is { } cancelledAt
                ? DateOnly.FromDateTime(cancelledAt.LocalDateTime)
                : payment.PaymentDate;
            var sortAt = payment.Status == PaymentStatus.Cancelled && payment.CancelledAt is { } cancelled
                ? cancelled.LocalDateTime
                : payment.PaymentDate.ToDateTime(TimeOnly.MinValue);

            items.Add(new ActivityItem(sortAt, activityDate, "Pago de suscripción", payment.AmountCents, status, color));
        }

        foreach (var expense in expenses.Where(item =>
                     item.Status is ExpenseStatus.Confirmed or ExpenseStatus.Cancelled))
        {
            var (status, color) = Ui.ExpenseBadge(expense.Status);
            var cancelled = expense.Status == ExpenseStatus.Cancelled;
            var activityDate = cancelled && expense.CancelledAt is { } cancelledAt
                ? DateOnly.FromDateTime(cancelledAt.LocalDateTime)
                : expense.ActualDate ?? expense.PlannedDate;
            var sortAt = cancelled && expense.CancelledAt is { } cancellation
                ? cancellation.LocalDateTime
                : activityDate.ToDateTime(TimeOnly.MinValue);

            items.Add(new ActivityItem(sortAt, activityDate, $"Gasto · {expense.Concept}", expense.AmountCents, status, color));
        }

        foreach (var income in incomes.Where(item =>
                     item.Status is IncomeStatus.Confirmed or IncomeStatus.Cancelled))
        {
            var (status, color) = Ui.IncomeBadge(income.Status);
            var cancelled = income.Status == IncomeStatus.Cancelled;
            var activityDate = cancelled && income.CancelledAt is { } cancelledAt
                ? DateOnly.FromDateTime(cancelledAt.LocalDateTime)
                : income.ActualDate ?? income.PlannedDate;
            var sortAt = cancelled && income.CancelledAt is { } cancellation
                ? cancellation.LocalDateTime
                : activityDate.ToDateTime(TimeOnly.MinValue);

            items.Add(new ActivityItem(sortAt, activityDate, $"Ingreso · {income.Concept}", income.AmountCents, status, color));
        }

        var recent = items.OrderByDescending(item => item.SortAt).Take(10).ToArray();
        _activity.Rows.Clear();
        _activityCount.Text = recent.Length == 0 ? "Sin actividad reciente" : $"{recent.Length} recientes";

        foreach (var item in recent)
        {
            var index = _activity.Rows.Add(
                Format.Date(item.Date),
                item.Concept,
                Format.Money(item.AmountCents),
                item.Status);
            Ui.Tint(_activity.Rows[index], "Estado", item.Color);
            _activity.Rows[index].Tag = item;
        }
    }

    private void UpdateContentLayout()
    {
        if (_scrollHost.ClientSize.Width <= 0)
        {
            return;
        }

        var scale = _scrollHost.DeviceDpi / 96f;
        var upcomingHeight = Math.Max((int)(300 * scale), _upcoming.Rows.Count * _upcoming.RowTemplate.Height + (int)(100 * scale));
        var activityHeight = Math.Max((int)(500 * scale), _activity.Rows.Count * _activity.RowTemplate.Height + (int)(100 * scale));
        var width = _scrollHost.ClientSize.Width;
        var height = upcomingHeight + activityHeight + (int)(14 * scale);

        _content.SuspendLayout();
        _content.RowStyles.Clear();
        _content.RowStyles.Add(new RowStyle(SizeType.Absolute, upcomingHeight));
        _content.RowStyles.Add(new RowStyle(SizeType.Absolute, activityHeight));
        _content.Size = new Size(width, height);
        _scrollHost.AutoScrollMinSize = new Size(width, height);
        _content.ResumeLayout(true);
    }

    private static Card BuildList(string title, DataGridView grid, Label counter)
    {
        var card = new Card
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18, 8, 18, 8),
            Margin = new Padding(0, 0, 0, 14)
        };
        card.Controls.Add(grid);
        card.Controls.Add(Ui.Header(title, counter));
        return card;
    }

    private static Label Counter(Color color) => new()
    {
        AutoSize = false,
        Dock = DockStyle.Fill,
        Font = Palette.Font(8.5f, FontStyle.Bold),
        ForeColor = color,
        TextAlign = ContentAlignment.MiddleRight,
        BackColor = Color.Transparent,
        Margin = Padding.Empty,
        Text = "Cargando datos..."
    };

    private sealed record ActivityItem(
        DateTime SortAt,
        DateOnly Date,
        string Concept,
        long AmountCents,
        string Status,
        Color Color);
}
