using SubTrack.Core.Contracts;
using SubTrack.Core.Domain.Entities;
using SubTrack.Core.Domain.Enums;
using SubTrack.WinForms.Theme;
using SubTrack.WinForms.UI;

namespace SubTrack.WinForms.Views;

internal sealed class DashboardView : ViewBase
{
    private readonly MonthStepper _month = new();

    private readonly Label _balance = ValueLabel(28f, FontStyle.Bold, Color.White);
    private readonly Label _balanceHint = Hint(8.5f, Color.FromArgb(146, 158, 168));
    private readonly Label _projected = ValueLabel(19f, FontStyle.Bold, Palette.Text);
    private readonly Label _projectedHint = Hint(8.5f, Palette.Muted);
    private readonly Label _active = ValueLabel(26f, FontStyle.Bold, Palette.Text);
    private readonly Label _activeHint = Hint(8.5f, Palette.Muted);

    private readonly Label _income = ValueLabel(15f, FontStyle.Bold, Palette.Text);
    private readonly Label _expenses = ValueLabel(15f, FontStyle.Bold, Palette.Text);
    private readonly Label _payments = ValueLabel(15f, FontStyle.Bold, Palette.Text);
    private readonly Label _total = ValueLabel(15f, FontStyle.Bold, Palette.Danger);

    private readonly DataGridView _upcoming = Ui.Grid(("Fecha", 26f), ("Suscripción", 50f), ("Importe", 24f));
    private readonly DataGridView _activity = Ui.Grid(("Fecha", 22f), ("Actividad", 42f), ("Importe", 20f), ("Estado", 16f));
    private readonly DataGridView _overdue = Ui.Grid(("Fecha", 26f), ("Suscripción", 50f), ("Importe", 24f));
    private readonly Label _upcomingCount = Counter(Palette.PrimaryDeep);
    private readonly Label _activityCount = Counter(Palette.Muted);
    private readonly Label _overdueCount = Counter(Palette.Danger);
    private readonly BarsChart _chart = new();
    private readonly Label _chartEmpty = Hint(9f, Palette.Muted, "No hay movimientos confirmados en los últimos 6 meses.");
    private readonly Label _summaryFigures = Ui.Label("Cargando datos...", 8.5f, Palette.Muted);
    private readonly Panel _contentPanel = new() { Dock = DockStyle.Fill, BackColor = Palette.Canvas };
    private Control? _overviewPanel;
    private Control? _summaryPanel;
    private Button? _summaryToggle;

    private Dictionary<Guid, string> _names = new();

    public DashboardView(ViewContext context)
        : base(context)
    {
    }

    public override string Title => "Resumen";

    public override string Subtitle => "Tu foto financiera del mes, sin ruido.";

    protected override void Build()
    {
        _month.MonthChanged += async () => await RefreshAsync();

        var caption = Ui.Caption("MES");
        caption.Margin = new Padding(4, 13, 10, 0);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Palette.Canvas,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };

        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 178));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 110));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        root.Controls.Add(
            Ui.Toolbar(
                new Control[] { caption, _month },
                new Control[]
                {
                    Ui.Action("Generar cobros", GenerateAsync, ButtonKind.Primary),
                    Ui.Action("Actualizar", RefreshAsync),
                    _summaryToggle = Ui.Action("Ver resumen", ToggleSummaryAsync)
                }),
            0,
            0);

        root.Controls.Add(BuildHero(), 0, 1);
        root.Controls.Add(BuildKpis(), 0, 2);
        _overviewPanel = BuildOverview();
        _summaryPanel = BuildSummaryPanel();
        _summaryPanel.Visible = false;
        _contentPanel.Controls.Add(_summaryPanel);
        _contentPanel.Controls.Add(_overviewPanel);
        root.Controls.Add(_contentPanel, 0, 3);

        Ui.Align(_upcoming, "Importe");
        Ui.Align(_overdue, "Importe");
        Ui.Align(_activity, "Importe");

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

        var result = await dashboardTask;
        if (Failed(result))
        {
            ClearDashboard();
            return;
        }

        var subscriptions = await subscriptionsTask;
        if (Failed(subscriptions))
        {
            _names.Clear();
        }
        else
        {
            _names = subscriptions.Value.ToDictionary(subscription => subscription.Id, subscription => subscription.Name);
        }

        var snapshot = result.Value;

        _balance.Text = Format.Currency(snapshot.CurrentBalanceCents);
        _balanceHint.Text = $"Inicial {Format.Currency(snapshot.OpeningBalanceCents)} · {Format.Month(snapshot.Month)}";

        _projected.Text = Format.Currency(snapshot.ProjectedSavingsCents);
        _projectedHint.Text = snapshot.ProjectedSavingsCents >= 0 ? "Al cierre del mes" : "Cierre en déficit";

        _active.Text = snapshot.ActiveSubscriptions.ToString();
        _activeHint.Text = snapshot.ActiveSubscriptions == 1
            ? "suscripción activa"
            : "suscripciones activas";

        _income.Text = Format.Money(snapshot.IncomeCents);
        _expenses.Text = Format.Money(snapshot.ExpensesCents);
        _payments.Text = Format.Money(snapshot.SubscriptionPaymentsCents);
        _total.Text = Format.Money(snapshot.TotalExpensesCents);
        _summaryFigures.Text =
            $"Ingresos {Format.Money(snapshot.IncomeCents)} · Gastos {Format.Money(snapshot.ExpensesCents)} · Suscripciones {Format.Money(snapshot.SubscriptionPaymentsCents)} · Egresos {Format.Money(snapshot.TotalExpensesCents)}";

        Fill(_upcoming, snapshot.UpcomingCharges);

        Fill(_overdue, snapshot.OverdueCharges);

        _overdueCount.Text = snapshot.OverdueCharges.Count == 0
            ? "Sin cargos vencidos"
            : $"{snapshot.OverdueCharges.Count} atrasados";

        _overdueCount.ForeColor = snapshot.OverdueCharges.Count == 0 ? Palette.PrimaryDeep : Palette.Danger;

        var payments = await paymentsTask;
        var expenses = await expensesTask;
        var incomes = await incomesTask;
        var activityAvailable = !payments.IsFailure && !expenses.IsFailure && !incomes.IsFailure;

        if (!activityAvailable)
        {
            if (payments.IsFailure) Failed(payments);
            if (expenses.IsFailure) Failed(expenses);
            if (incomes.IsFailure) Failed(incomes);
            _activity.Rows.Clear();
            _activityCount.Text = "No se pudo cargar";
        }
        else
        {
            FillActivity(payments.Value.Items, expenses.Value.Items, incomes.Value.Items);
        }

        await LoadChartAsync();
    }

    private async Task LoadChartAsync()
    {
        var months = new List<DateOnly>();
        var start = _month.Month.ToDateTime(TimeOnly.MinValue);

        for (var offset = 5; offset >= 0; offset--)
        {
            months.Add(DateOnly.FromDateTime(start.AddMonths(-offset)));
        }

        var results = await Task.WhenAll(months.Select(month =>
            Context.Services.Dashboard.GetAsync(new GetDashboardRequest(month))));

        var failures = results.Where(result => result.IsFailure).ToArray();
        if (failures.Length > 0)
        {
            _chart.SetData(Array.Empty<BarsChart.Bar>());
            _chart.Visible = false;
            _chartEmpty.Visible = true;
            Notify($"No se pudo cargar el gráfico: {Describe(failures[0].Errors)}", true);
            return;
        }

        var bars = months.Zip(results, (month, result) => new BarsChart.Bar(
            Format.MonthShort(month),
            result.Value.IncomeCents,
            result.Value.TotalExpensesCents)).ToArray();

        _chart.SetData(bars);
        var hasMovements = bars.Any(bar => bar.IncomeCents != 0 || bar.ExpenseCents != 0);
        _chart.Visible = hasMovements;
        _chartEmpty.Visible = !hasMovements;
    }

    private Task ToggleSummaryAsync()
    {
        var showSummary = _summaryPanel is not { Visible: true };
        if (_summaryPanel is null || _overviewPanel is null)
        {
            return Task.CompletedTask;
        }

        _summaryPanel.Visible = showSummary;
        _overviewPanel.Visible = !showSummary;
        if (_summaryToggle is not null)
        {
            _summaryToggle.Text = showSummary ? "Ocultar resumen" : "Ver resumen";
        }

        return Task.CompletedTask;
    }

    private void ClearDashboard()
    {
        _balance.Text = "—";
        _balanceHint.Text = "No se pudieron cargar los datos.";
        _projected.Text = "—";
        _projectedHint.Text = "No disponible";
        _active.Text = "—";
        _activeHint.Text = "No disponible";
        _income.Text = "—";
        _expenses.Text = "—";
        _payments.Text = "—";
        _total.Text = "—";
        _upcoming.Rows.Clear();
        _activity.Rows.Clear();
        _overdue.Rows.Clear();
        _upcomingCount.Text = "No se pudo cargar";
        _activityCount.Text = "No se pudo cargar";
        _overdueCount.Text = "No se pudo cargar";
        _chart.SetData(Array.Empty<BarsChart.Bar>());
        _chart.Visible = false;
        _chartEmpty.Visible = true;
    }

    private async Task GenerateAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        var last = new DateOnly(
            _month.Month.Year,
            _month.Month.Month,
            DateTime.DaysInMonth(_month.Month.Year, _month.Month.Month));

        if (last < today)
        {
            Notify("No se pueden generar cobros de un mes pasado.", true);
            return;
        }

        var subscriptions = await Context.Services.Subscriptions.GetAllAsync();

        if (Failed(subscriptions))
        {
            return;
        }

        var created = 0;
        var failures = new List<string>();

        foreach (var subscription in subscriptions.Value.Where(item => item.IsActive))
        {
            var result = await Context.Services.Charges.GenerateAsync(
                new GenerateChargesRequest(subscription.Id, last));

            if (result.IsSuccess)
            {
                created += result.Value.Count;
            }
            else
            {
                failures.Add($"{subscription.Name}: {Describe(result.Errors)}");
            }
        }

        if (failures.Count > 0)
        {
            Notify(failures[0], true);
        }
        else
        {
            Notify(created == 0 ? "No había cobros nuevos por generar." : $"{created} cobros generados.");
        }

        await DataChangedAsync(DataChangeKind.Charges);
    }

    private void Fill(DataGridView grid, IReadOnlyList<ScheduledCharge> charges)
    {
        grid.Rows.Clear();

        _upcomingCount.Text = charges.Count == 0
            ? "Sin cargos pendientes próximos"
            : $"{charges.Count} próximos";

        foreach (var charge in charges)
        {
            var name = _names.TryGetValue(charge.SubscriptionId, out var value)
                ? value
                : "Suscripción no disponible";

            var index = grid.Rows.Add(
                Format.Date(charge.ScheduledDate),
                name,
                Format.Money(charge.AmountCents));

            grid.Rows[index].Tag = charge;
        }
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

            items.Add(new ActivityItem(
                sortAt,
                activityDate,
                "Pago de suscripción",
                payment.AmountCents,
                status,
                color));
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

            items.Add(new ActivityItem(
                sortAt,
                activityDate,
                $"Gasto · {expense.Concept}",
                expense.AmountCents,
                status,
                color));
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

            items.Add(new ActivityItem(
                sortAt,
                activityDate,
                $"Ingreso · {income.Concept}",
                income.AmountCents,
                status,
                color));
        }

        var recent = items
            .OrderByDescending(item => item.SortAt)
            .Take(10)
            .ToArray();

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

    private Control BuildHero()
    {
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Palette.Canvas,
            Margin = new Padding(0, 0, 0, 14)
        };

        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));

        var hero = new Card
        {
            Dock = DockStyle.Fill,
            Radius = 18,
            ShowBorder = false,
            Margin = new Padding(0, 0, 14, 0),
            Padding = new Padding(24, 20, 24, 18)
        };

        hero.Fill(Palette.Ink);

        var heroLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Palette.Ink,
            Margin = new Padding(0)
        };

        heroLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        heroLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        heroLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var balanceCaption = Ui.Caption("SALDO ACTUAL");
        balanceCaption.ForeColor = Color.FromArgb(150, 162, 172);

        _balance.Text = "—";
        _balanceHint.Text = "Cargando datos...";

        heroLayout.Controls.Add(balanceCaption, 0, 0);
        heroLayout.Controls.Add(_balance, 0, 1);
        heroLayout.Controls.Add(_balanceHint, 0, 2);
        hero.Controls.Add(heroLayout);

        var projected = new Card
        {
            Dock = DockStyle.Fill,
            Radius = 18,
            ShowBorder = false,
            Margin = new Padding(0, 0, 14, 0),
            Padding = new Padding(20, 18, 20, 16)
        };

        projected.Fill(Palette.PrimarySoft);

        var projectedLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Palette.PrimarySoft,
            Margin = new Padding(0)
        };

        projectedLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        projectedLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        projectedLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var projectedCaption = Ui.Caption("AHORRO PROYECTADO");
        projectedCaption.ForeColor = Palette.PrimaryDeep;

        _projected.Text = "—";
        _projectedHint.Text = "Cargando datos...";

        projectedLayout.Controls.Add(projectedCaption, 0, 0);
        projectedLayout.Controls.Add(_projected, 0, 1);
        projectedLayout.Controls.Add(_projectedHint, 0, 2);
        projected.Controls.Add(projectedLayout);

        var activeCard = new Card
        {
            Dock = DockStyle.Fill,
            Radius = 18,
            Margin = new Padding(0),
            Padding = new Padding(20, 18, 20, 16)
        };

        var activeLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Palette.Surface,
            Margin = new Padding(0)
        };

        activeLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        activeLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        activeLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _active.Text = "—";
        _activeHint.Text = "Cargando datos...";

        activeLayout.Controls.Add(Ui.Caption("SUSCRIPCIONES"), 0, 0);
        activeLayout.Controls.Add(_active, 0, 1);
        activeLayout.Controls.Add(_activeHint, 0, 2);
        activeCard.Controls.Add(activeLayout);

        row.Controls.Add(hero, 0, 0);
        row.Controls.Add(projected, 1, 0);
        row.Controls.Add(activeCard, 2, 0);

        return row;
    }

    private Control BuildKpis()
    {
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1,
            BackColor = Palette.Canvas,
            Margin = new Padding(0, 0, 0, 14)
        };

        for (var index = 0; index < 4; index++)
        {
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        }

        var income = Kpi("INGRESOS CONFIRMADOS", _income, "del mes");
        var expenses = Kpi("GASTOS CONFIRMADOS", _expenses, "del mes");
        var payments = Kpi("PAGOS DE SUSCRIPCIÓN", _payments, "cobros pagados");
        var total = Kpi("TOTAL EGRESOS", _total, "gastos + suscripciones");

        _income.Text = "—";
        _expenses.Text = "—";
        _payments.Text = "—";
        _total.Text = "—";
        _upcomingCount.Text = "Cargando datos...";
        _activityCount.Text = "Cargando datos...";
        _overdueCount.Text = "Cargando datos...";

        income.Margin = new Padding(0, 0, 14, 0);
        expenses.Margin = new Padding(0, 0, 14, 0);
        payments.Margin = new Padding(0, 0, 14, 0);

        row.Controls.Add(income, 0, 0);
        row.Controls.Add(expenses, 1, 0);
        row.Controls.Add(payments, 2, 0);
        row.Controls.Add(total, 3, 0);

        return row;
    }

    private Control BuildOverview()
    {
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Palette.Canvas,
            Margin = new Padding(0)
        };

        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));

        _upcoming.RowTemplate.Height = 38;
        _upcoming.ColumnHeadersHeight = 34;
        _activity.RowTemplate.Height = 38;
        _activity.ColumnHeadersHeight = 34;

        row.Controls.Add(BuildList("Próximos cargos", _upcoming, _upcomingCount, true), 0, 0);
        row.Controls.Add(BuildList("Actividad reciente", _activity, _activityCount, false), 1, 0);

        return row;
    }

    private Control BuildSummaryPanel()
    {
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Palette.Canvas,
            Margin = new Padding(0)
        };

        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));

        var chartCard = new Card
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 14, 0),
            Padding = new Padding(20, 12, 20, 10)
        };

        _summaryFigures.TextAlign = ContentAlignment.MiddleRight;
        _chart.Dock = DockStyle.Fill;
        _chart.Margin = new Padding(0);
        _chartEmpty.Dock = DockStyle.Fill;
        _chartEmpty.TextAlign = ContentAlignment.MiddleCenter;

        chartCard.Controls.Add(_chart);
        chartCard.Controls.Add(_chartEmpty);
        chartCard.Controls.Add(Ui.Header("Últimos 6 meses", _summaryFigures));

        _overdue.RowTemplate.Height = 38;
        _overdue.ColumnHeadersHeight = 34;

        row.Controls.Add(chartCard, 0, 0);
        row.Controls.Add(BuildList("Cobros vencidos", _overdue, _overdueCount, false), 1, 0);

        return row;
    }

    private static Control BuildList(string title, DataGridView grid, Label counter, bool gap)
    {
        var card = new Card
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18, 8, 18, 8),
            Margin = gap ? new Padding(0, 0, 0, 14) : new Padding(0)
        };

        card.Controls.Add(grid);
        card.Controls.Add(Ui.Header(title, counter));

        return card;
    }

    private static Card Kpi(string caption, Label value, string hint)
    {
        var card = new Card
        {
            Dock = DockStyle.Fill,
            Radius = 14,
            Padding = new Padding(18, 12, 18, 10),
            Margin = new Padding(0)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Palette.Surface,
            Margin = new Padding(0)
        };

        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        layout.Controls.Add(Ui.Caption(caption), 0, 0);
        layout.Controls.Add(value, 0, 1);
        layout.Controls.Add(Hint(8f, Palette.Muted, hint), 0, 2);

        card.Controls.Add(layout);

        return card;
    }

    private static Label ValueLabel(float size, FontStyle style, Color color) => new()
    {
        AutoSize = false,
        Dock = DockStyle.Fill,
        Font = Palette.Font(size, style),
        ForeColor = color,
        TextAlign = ContentAlignment.MiddleLeft,
        BackColor = Color.Transparent,
        Margin = new Padding(0)
    };

    private static Label Hint(float size, Color color, string text = "—") => new()
    {
        AutoSize = false,
        Dock = DockStyle.Fill,
        Font = Palette.Font(size),
        ForeColor = color,
        TextAlign = ContentAlignment.MiddleLeft,
        BackColor = Color.Transparent,
        Text = text,
        Margin = new Padding(0)
    };

    private static Label Counter(Color color) => new()
    {
        AutoSize = false,
        Dock = DockStyle.Fill,
        Font = Palette.Font(8.5f, FontStyle.Bold),
        ForeColor = color,
        TextAlign = ContentAlignment.MiddleRight,
        BackColor = Color.Transparent,
        Margin = new Padding(0)
    };

    private sealed record ActivityItem(
        DateTime SortAt,
        DateOnly Date,
        string Concept,
        long AmountCents,
        string Status,
        Color Color);
}
