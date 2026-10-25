using SubTrack.Core.Contracts;
using SubTrack.Core.Domain.Entities;
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
    private readonly DataGridView _overdue = Ui.Grid(("Fecha", 26f), ("Suscripción", 50f), ("Importe", 24f));
    private readonly Label _upcomingCount = Counter(Palette.PrimaryDeep);
    private readonly Label _overdueCount = Counter(Palette.Danger);
    private readonly BarsChart _chart = new();

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
                    Ui.Action("Actualizar", RefreshAsync)
                }),
            0,
            0);

        root.Controls.Add(BuildHero(), 0, 1);
        root.Controls.Add(BuildKpis(), 0, 2);
        root.Controls.Add(BuildBottom(), 0, 3);

        Ui.Align(_upcoming, "Importe");
        Ui.Align(_overdue, "Importe");

        Controls.Add(root);
    }

    protected override async Task LoadAsync()
    {
        var subscriptions = await Context.Services.Subscriptions.GetAllAsync();

        if (Failed(subscriptions))
        {
            return;
        }

        _names = subscriptions.Value.ToDictionary(subscription => subscription.Id, subscription => subscription.Name);
        var active = subscriptions.Value.Count(subscription => subscription.IsActive);

        var result = await Context.Services.Dashboard.GetAsync(new GetDashboardRequest(_month.Month));

        if (Failed(result))
        {
            return;
        }

        var snapshot = result.Value;

        _balance.Text = Format.Currency(snapshot.CurrentBalanceCents);
        _balanceHint.Text = $"Inicial {Format.Currency(snapshot.OpeningBalanceCents)} · {Format.Month(snapshot.Month)}";

        _projected.Text = Format.Currency(snapshot.ProjectedSavingsCents);
        _projectedHint.Text = snapshot.ProjectedSavingsCents >= 0 ? "Al cierre del mes" : "Cierre en déficit";

        _active.Text = active.ToString();
        _activeHint.Text = snapshot.UpcomingCharges.Count == 0
            ? "Sin cobros por venir"
            : $"{snapshot.UpcomingCharges.Count} cobros por venir";

        _income.Text = Format.Money(snapshot.IncomeCents);
        _expenses.Text = Format.Money(snapshot.ExpensesCents);
        _payments.Text = Format.Money(snapshot.SubscriptionPaymentsCents);
        _total.Text = Format.Money(snapshot.TotalExpensesCents);

        Fill(_upcoming, snapshot.UpcomingCharges);
        Fill(_overdue, snapshot.OverdueCharges);

        _upcomingCount.Text = snapshot.UpcomingCharges.Count == 0
            ? "Sin pendientes"
            : $"{snapshot.UpcomingCharges.Count} por cobrar";

        _overdueCount.Text = snapshot.OverdueCharges.Count == 0
            ? "Al día"
            : $"{snapshot.OverdueCharges.Count} atrasados";

        _overdueCount.ForeColor = snapshot.OverdueCharges.Count == 0 ? Palette.PrimaryDeep : Palette.Danger;

        await LoadChartAsync();
    }

    private async Task LoadChartAsync()
    {
        var bars = new List<BarsChart.Bar>();
        var start = _month.Month.ToDateTime(TimeOnly.MinValue);

        for (var offset = 5; offset >= 0; offset--)
        {
            var month = DateOnly.FromDateTime(start.AddMonths(-offset));
            var result = await Context.Services.Dashboard.GetAsync(new GetDashboardRequest(month));

            if (!result.IsSuccess)
            {
                continue;
            }

            bars.Add(new BarsChart.Bar(
                Format.MonthShort(month),
                result.Value.IncomeCents,
                result.Value.TotalExpensesCents));
        }

        _chart.SetData(bars);
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

        await RefreshAsync();
    }

    private void Fill(DataGridView grid, IReadOnlyList<ScheduledCharge> charges)
    {
        grid.Rows.Clear();

        foreach (var charge in charges)
        {
            var name = _names.TryGetValue(charge.SubscriptionId, out var value) ? value : "—";

            var index = grid.Rows.Add(
                Format.Date(charge.ScheduledDate),
                name,
                Format.Money(charge.AmountCents));

            grid.Rows[index].Tag = charge;
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

        _balance.Text = "S/ 0.00";
        _balanceHint.Text = "—";

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

        _projected.Text = "S/ 0.00";
        _projectedHint.Text = "Al cierre del mes";

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

        _active.Text = "0";
        _activeHint.Text = "suscripciones activas";

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

        income.Margin = new Padding(0, 0, 14, 0);
        expenses.Margin = new Padding(0, 0, 14, 0);
        payments.Margin = new Padding(0, 0, 14, 0);

        row.Controls.Add(income, 0, 0);
        row.Controls.Add(expenses, 1, 0);
        row.Controls.Add(payments, 2, 0);
        row.Controls.Add(total, 3, 0);

        return row;
    }

    private Control BuildBottom()
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

        var chartHint = Ui.Label("Ingresos vs egresos", 8.5f, Palette.Muted);
        chartHint.TextAlign = ContentAlignment.MiddleRight;

        _chart.Dock = DockStyle.Fill;
        _chart.Margin = new Padding(0);

        chartCard.Controls.Add(_chart);
        chartCard.Controls.Add(Ui.Header("Últimos 6 meses", chartHint));

        _upcoming.RowTemplate.Height = 38;
        _upcoming.ColumnHeadersHeight = 34;
        _overdue.RowTemplate.Height = 38;
        _overdue.ColumnHeadersHeight = 34;

        var lists = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Palette.Canvas,
            Margin = new Padding(0)
        };

        lists.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        lists.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        lists.Controls.Add(BuildList("Próximos cobros", _upcoming, _upcomingCount, true), 0, 0);
        lists.Controls.Add(BuildList("Cobros atrasados", _overdue, _overdueCount, false), 0, 1);

        row.Controls.Add(chartCard, 0, 0);
        row.Controls.Add(lists, 1, 0);

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
}
