using System.Runtime.InteropServices;
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
    private readonly Label _projected = ValueLabel(28f, FontStyle.Bold, Palette.Text);
    private readonly Label _projectedHint = Hint(8.5f, Palette.Muted);
    private readonly Label _active = ValueLabel(26f, FontStyle.Bold, Palette.Text);
    private readonly Label _activeHint = Hint(8.5f, Palette.Muted);

    private readonly Label _income = ValueLabel(15f, FontStyle.Bold, Palette.Text);
    private readonly Label _expenses = ValueLabel(15f, FontStyle.Bold, Palette.Text);
    private readonly Label _payments = ValueLabel(15f, FontStyle.Bold, Palette.Text);
    private readonly Label _total = ValueLabel(15f, FontStyle.Bold, Palette.Danger);

    private readonly DataGridView _overdue = Ui.Grid(("Fecha", 26f), ("Suscripción", 50f), ("Importe", 24f));
    private readonly Label _overdueCount = Counter(Palette.Danger);
    private readonly BarsChart _chart = new();
    private readonly Label _chartEmpty = Hint(9f, Palette.Muted, "No hay movimientos confirmados en los últimos 6 meses.");
    private readonly Label _summaryFigures = Ui.Label("Cargando datos...", 8.5f, Palette.Muted);
    private readonly Label _summaryBalance = Ui.Label("Saldo disponible — · Ahorro proyectado —", 8.5f, Palette.Muted);
    private readonly Panel _contentPanel = new() { Dock = DockStyle.Fill, BackColor = Palette.Canvas };
    private readonly DashboardScrollPanel _dashboardScrollHost = new()
    {
        Dock = DockStyle.Fill,
        AutoScroll = true,
        BackColor = Palette.Canvas
    };
    private Control? _dashboardPanel;
    private Control? _summaryPanel;
    private Control? _dashboardMainPanel;
    private TableLayoutPanel? _heroLayout;
    private Control[]? _heroCards;
    private bool _heroCardsStacked;
    private Card? _balanceCard;
    private Card? _projectedCard;
    private Card? _subscriptionsCard;
    private TableLayoutPanel? _subscriptionsLayout;
    private Label? _subscriptionsTitle;
    private TableLayoutPanel? _kpisLayout;
    private Control[]? _kpiCards;
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
            RowCount = 2,
            BackColor = Palette.Canvas,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };

        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
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

        _dashboardPanel = BuildDashboardPanel();
        _summaryPanel = BuildSummaryPanel();
        _summaryPanel.Visible = false;
        _contentPanel.Controls.Add(_summaryPanel);
        _contentPanel.Controls.Add(_dashboardPanel);
        root.Controls.Add(_contentPanel, 0, 1);

        Ui.Align(_overdue, "Importe");

        Controls.Add(root);
    }

    protected override async Task LoadAsync()
    {
        var dashboardTask = Context.Services.Dashboard.GetAsync(new GetDashboardRequest(_month.Month));
        var subscriptionsTask = Context.Services.Subscriptions.GetAllAsync();
        await Task.WhenAll(dashboardTask, subscriptionsTask);

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
            _names = subscriptions.Value.ToDictionary(item => item.Id, item => item.Name);
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
        _summaryBalance.Text =
            $"Saldo disponible {Format.Currency(snapshot.CurrentBalanceCents)} · Ahorro proyectado {Format.Currency(snapshot.ProjectedSavingsCents)}";

        Fill(snapshot.OverdueCharges);

        _overdueCount.Text = snapshot.OverdueCharges.Count == 0
            ? "Sin cargos vencidos"
            : $"{snapshot.OverdueCharges.Count} atrasados";

        _overdueCount.ForeColor = snapshot.OverdueCharges.Count == 0 ? Palette.PrimaryDeep : Palette.Danger;

        await LoadChartAsync();
    }

    private void Fill(IReadOnlyList<ScheduledCharge> charges)
    {
        _overdue.Rows.Clear();

        foreach (var charge in charges)
        {
            var name = _names.TryGetValue(charge.SubscriptionId, out var value)
                ? value
                : "Suscripción no disponible";
            var index = _overdue.Rows.Add(
                Format.Date(charge.ScheduledDate),
                name,
                Format.Money(charge.AmountCents));
            _overdue.Rows[index].Tag = charge;
        }
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
        if (_summaryPanel is null || _dashboardPanel is null)
        {
            return Task.CompletedTask;
        }

        _summaryPanel.Visible = showSummary;
        _dashboardPanel.Visible = !showSummary;
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
        _overdue.Rows.Clear();
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

    private Control BuildHero()
    {
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Palette.Canvas,
            Margin = Padding.Empty
        };

        var balanceCaption = Ui.Caption("SALDO ACTUAL");
        balanceCaption.ForeColor = Color.FromArgb(150, 162, 172);

        _balance.Text = "—";
        _balanceHint.Text = "Cargando datos...";

        var projectedCaption = Ui.Caption("AHORRO PROYECTADO");
        projectedCaption.ForeColor = Palette.PrimaryDeep;

        _projected.Text = "—";
        _projectedHint.Text = "Cargando datos...";

        _balanceCard = BuildFinancialIndicator(
            balanceCaption,
            _balance,
            _balanceHint,
            Palette.Ink,
            new Padding(24, 12, 24, 12),
            new Padding(0, 0, 7, 0),
            28f);
        _projectedCard = BuildFinancialIndicator(
            projectedCaption,
            _projected,
            _projectedHint,
            Palette.PrimarySoft,
            new Padding(24, 12, 24, 12),
            new Padding(7, 0, 0, 0),
            28f);

        _subscriptionsCard = (Card)BuildSubscriptionsInfo();
        _heroLayout = row;
        _heroCards = [_balanceCard, _projectedCard, _subscriptionsCard];
        row.SizeChanged += (_, _) => UpdateHeroLayout();
        row.DpiChangedAfterParent += (_, _) => UpdateHeroLayout();
        _balance.TextChanged += (_, _) => UpdateHeroLayout();
        _projected.TextChanged += (_, _) => UpdateHeroLayout();
        _active.TextChanged += (_, _) => UpdateHeroLayout();
        _activeHint.TextChanged += (_, _) => UpdateHeroLayout();
        UpdateHeroLayout();

        return row;
    }

    private void UpdateHeroLayout()
    {
        if (_heroLayout is null || _heroCards is null || _heroLayout.ClientSize.Width <= 0)
        {
            return;
        }

        var scale = _heroLayout.DeviceDpi / 96f;
        var subscriptionWidth = GetSubscriptionsCardWidth(scale);
        var financialWidth = GetFinancialCardMinimumWidth(scale);
        var gap = (int)(8 * scale);
        var minimumHorizontalWidth = (financialWidth * 2) + subscriptionWidth + (gap * 2);
        var stackCards = _heroLayout.ClientSize.Width < minimumHorizontalWidth;
        _heroCardsStacked = stackCards;
        var columnCount = stackCards ? 1 : 3;
        var rowCount = stackCards ? 3 : 1;

        _heroLayout.SuspendLayout();
        _heroLayout.ColumnCount = columnCount;
        _heroLayout.RowCount = rowCount;
        _heroLayout.ColumnStyles.Clear();
        _heroLayout.RowStyles.Clear();

        if (stackCards)
        {
            _heroLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (var row = 0; row < rowCount; row++)
            {
                _heroLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / rowCount));
            }
        }
        else
        {
            _heroLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            _heroLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            _heroLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, subscriptionWidth));
            _heroLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        }

        for (var index = 0; index < _heroCards.Length; index++)
        {
            var row = stackCards ? index : 0;
            var column = stackCards ? 0 : index;
            _heroCards[index].Margin = new Padding(
                0,
                0,
                stackCards || index == _heroCards.Length - 1 ? 0 : gap,
                stackCards && index < _heroCards.Length - 1 ? gap : 0);

            if (_heroCards[index].Parent != _heroLayout)
            {
                _heroLayout.Controls.Add(_heroCards[index], column, row);
            }
            else
            {
                _heroLayout.SetCellPosition(
                    _heroCards[index],
                    new TableLayoutPanelCellPosition(column, row));
            }

            _heroLayout.SetRowSpan(_heroCards[index], 1);
        }

        _heroLayout.ResumeLayout(true);
        UpdateDashboardLayout();
    }

    private int GetFinancialCardMinimumWidth(float scale)
    {
        var smallestAmountFont = Palette.Font(17f, FontStyle.Bold);
        var balanceWidth = TextRenderer.MeasureText(_balance.Text, smallestAmountFont).Width;
        var projectedWidth = TextRenderer.MeasureText(_projected.Text, smallestAmountFont).Width;
        var horizontalPadding = (int)(24 * scale);
        return Math.Max(balanceWidth, projectedWidth) + horizontalPadding;
    }

    private int GetSubscriptionsCardWidth(float scale)
    {
        var titleWidth = TextRenderer.MeasureText("Suscripciones activas", Palette.Font(9f, FontStyle.Bold)).Width;
        var hintWidth = TextRenderer.MeasureText(_activeHint.Text, Palette.Font(8f)).Width;
        var countWidth = TextRenderer.MeasureText(_active.Text, Palette.Font(18f, FontStyle.Bold)).Width;
        var leftWidth = Math.Max(titleWidth, hintWidth);
        var contentWidth = Math.Max(leftWidth / 0.76f, countWidth / 0.24f);
        return (int)Math.Ceiling(contentWidth + (20 * scale));
    }

    private Card BuildFinancialIndicator(
        Label caption,
        Label value,
        Label hint,
        Color fill,
        Padding padding,
        Padding margin,
        float preferredFontSize)
    {
        var card = new Card
        {
            Dock = DockStyle.Fill,
            Radius = 18,
            ShowBorder = false,
            Margin = margin,
            Padding = padding
        };
        card.Fill(fill);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = fill,
            Margin = new Padding(0)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(caption, 0, 0);
        layout.Controls.Add(value, 0, 1);
        layout.Controls.Add(hint, 0, 2);
        card.Controls.Add(layout);
        card.SizeChanged += (_, _) => UpdateFinancialIndicator(card, value, preferredFontSize);
        value.TextChanged += (_, _) => UpdateFinancialIndicator(card, value, preferredFontSize);
        card.ParentChanged += (_, _) => UpdateFinancialIndicator(card, value, preferredFontSize);
        card.DpiChangedAfterParent += (_, _) => UpdateFinancialIndicator(card, value, preferredFontSize);

        return card;
    }

    private Control BuildKpis()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            BackColor = Palette.Canvas,
            Margin = Padding.Empty
        };
        _kpisLayout = layout;

        var income = Kpi("INGRESOS CONFIRMADOS", _income, "del mes");
        var expenses = Kpi("GASTOS CONFIRMADOS", _expenses, "del mes");
        var payments = Kpi("PAGOS DE SUSCRIPCIÓN", _payments, "cobros pagados");
        var total = Kpi("TOTAL EGRESOS", _total, "gastos + suscripciones");

        _income.Text = "—";
        _expenses.Text = "—";
        _payments.Text = "—";
        _total.Text = "—";
        _overdueCount.Text = "Cargando datos...";

        _kpiCards = [income, expenses, payments, total];
        layout.SizeChanged += (_, _) => UpdateKpisLayout();
        layout.DpiChangedAfterParent += (_, _) => UpdateKpisLayout();
        UpdateKpisLayout();

        return layout;
    }

    private void UpdateKpisLayout()
    {
        if (_kpisLayout is null || _kpiCards is null || _kpisLayout.ClientSize.Width <= 0)
        {
            return;
        }

        var scale = _kpisLayout.DeviceDpi / 96f;
        var twoColumns = _kpisLayout.ClientSize.Width >= (int)(480 * scale);
        var columnCount = twoColumns ? 2 : 1;
        var rowCount = twoColumns ? 2 : 4;

        _kpisLayout.SuspendLayout();
        _kpisLayout.ColumnCount = columnCount;
        _kpisLayout.RowCount = rowCount;
        _kpisLayout.ColumnStyles.Clear();
        _kpisLayout.RowStyles.Clear();

        for (var column = 0; column < columnCount; column++)
        {
            _kpisLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / columnCount));
        }

        for (var row = 0; row < rowCount; row++)
        {
            _kpisLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / rowCount));
        }

        var horizontalGap = (int)(8 * scale);
        var verticalGap = (int)(5 * scale);
        for (var index = 0; index < _kpiCards.Length; index++)
        {
            var row = twoColumns ? index / 2 : index;
            var column = twoColumns ? index % 2 : 0;
            _kpiCards[index].Margin = new Padding(
                0,
                0,
                column == 0 && twoColumns ? horizontalGap : 0,
                row < rowCount - 1 ? verticalGap : 0);
            if (_kpiCards[index].Parent != _kpisLayout)
            {
                _kpisLayout.Controls.Add(_kpiCards[index], column, row);
            }
            else
            {
                _kpisLayout.SetCellPosition(
                    _kpiCards[index],
                    new TableLayoutPanelCellPosition(column, row));
            }
        }

        _kpisLayout.ResumeLayout(true);
    }

    private Control BuildDashboardPanel()
    {
        _dashboardMainPanel = BuildDashboardMainPanel();
        _dashboardScrollHost.Controls.Add(_dashboardMainPanel);
        _dashboardScrollHost.SizeChanged += (_, _) => UpdateDashboardLayout();
        UpdateDashboardLayout();

        return _dashboardScrollHost;
    }

    private Control BuildDashboardMainPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Palette.Canvas,
            Margin = new Padding(0)
        };

        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 38));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 62));
        panel.Controls.Add(BuildHero(), 0, 0);
        panel.Controls.Add(BuildKpis(), 0, 1);

        return panel;
    }

    private Control BuildSubscriptionsInfo()
    {
        _subscriptionsCard = new Card
        {
            Dock = DockStyle.Fill,
            Radius = 14,
            Padding = new Padding(10, 3, 10, 3),
            Margin = Padding.Empty
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            BackColor = Palette.Surface,
            Margin = new Padding(0)
        };
        _subscriptionsLayout = layout;

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 78));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 58));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 42));

        _active.Text = "—";
        _activeHint.Text = "Cargando datos...";
        _activeHint.TextAlign = ContentAlignment.MiddleLeft;
        _active.TextAlign = ContentAlignment.MiddleRight;
        _active.Margin = Padding.Empty;
        _activeHint.Margin = Padding.Empty;
        _activeHint.Font = Palette.Font(8f);

        _subscriptionsTitle = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            Font = Palette.Font(9f, FontStyle.Bold),
            ForeColor = Palette.Text,
            Text = "Suscripciones activas",
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Color.Transparent,
            Margin = Padding.Empty
        };
        layout.Controls.Add(_subscriptionsTitle, 0, 0);
        layout.Controls.Add(_activeHint, 0, 1);
        layout.Controls.Add(_active, 1, 0);
        layout.SetRowSpan(_active, 2);
        _subscriptionsCard.Controls.Add(layout);
        _subscriptionsCard.SizeChanged += (_, _) => UpdateSubscriptionsLayout();
        _subscriptionsCard.DpiChangedAfterParent += (_, _) => UpdateSubscriptionsLayout();
        _active.TextChanged += (_, _) => UpdateSubscriptionsLayout();
        _activeHint.TextChanged += (_, _) => UpdateSubscriptionsLayout();

        return _subscriptionsCard;
    }

    private void UpdateFinancialIndicator(Card card, Label value, float preferredFontSize)
    {
        if (card.ClientSize.Width <= 0)
        {
            return;
        }

        var dpiScale = card.DeviceDpi / 96f;
        var horizontalPadding = Math.Clamp(card.ClientSize.Width / 16, (int)(12 * dpiScale), (int)(24 * dpiScale));
        var verticalPadding = (int)(12 * dpiScale);
        card.Padding = new Padding(horizontalPadding, verticalPadding, horizontalPadding, verticalPadding);
        if (card == _balanceCard)
        {
            card.Margin = new Padding(0, 0, (int)(7 * dpiScale), 0);
        }
        else if (card == _projectedCard)
        {
            card.Margin = new Padding((int)(7 * dpiScale), 0, 0, 0);
        }

        var availableWidth = Math.Max(1, card.ClientSize.Width - card.Padding.Horizontal);
        var minimumFontSize = 17f;
        var fontSize = preferredFontSize;

        while (fontSize > minimumFontSize &&
               TextRenderer.MeasureText(value.Text, Palette.Font(fontSize, FontStyle.Bold)).Width > availableWidth)
        {
            fontSize -= 0.5f;
        }

        value.Font = Palette.Font(Math.Max(fontSize, minimumFontSize), FontStyle.Bold);
    }

    private void UpdateSubscriptionsLayout()
    {
        if (_subscriptionsCard is null || _subscriptionsLayout is null)
        {
            return;
        }

        var dpiScale = _subscriptionsCard.DeviceDpi / 96f;
        var horizontalLayout = _subscriptionsCard.ClientSize.Width >= GetSubscriptionsCardWidth(dpiScale);
        _subscriptionsCard.Padding = new Padding(
            (int)(10 * dpiScale),
            (int)(3 * dpiScale),
            (int)(10 * dpiScale),
            (int)(3 * dpiScale));

        _subscriptionsLayout.SuspendLayout();
        _subscriptionsLayout.ColumnCount = horizontalLayout ? 2 : 1;
        _subscriptionsLayout.RowCount = horizontalLayout ? 2 : 3;
        _subscriptionsLayout.ColumnStyles.Clear();
        _subscriptionsLayout.RowStyles.Clear();

        if (horizontalLayout)
        {
            _subscriptionsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 76));
            _subscriptionsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24));
            _subscriptionsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 58));
            _subscriptionsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 42));
            _subscriptionsLayout.SetCellPosition(_subscriptionsTitle!, new TableLayoutPanelCellPosition(0, 0));
            _subscriptionsLayout.SetCellPosition(_activeHint, new TableLayoutPanelCellPosition(0, 1));
            _subscriptionsLayout.SetCellPosition(_active, new TableLayoutPanelCellPosition(1, 0));
            _subscriptionsLayout.SetRowSpan(_active, 2);
        }
        else
        {
            _subscriptionsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            _subscriptionsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _subscriptionsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _subscriptionsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _subscriptionsLayout.SetCellPosition(_subscriptionsTitle!, new TableLayoutPanelCellPosition(0, 0));
            _subscriptionsLayout.SetCellPosition(_activeHint, new TableLayoutPanelCellPosition(0, 1));
            _subscriptionsLayout.SetCellPosition(_active, new TableLayoutPanelCellPosition(0, 2));
            _activeHint.TextAlign = ContentAlignment.MiddleLeft;
            _subscriptionsLayout.SetRowSpan(_active, 1);
        }

        _active.Font = Palette.Font(horizontalLayout ? 18f : 21f, FontStyle.Bold);
        _subscriptionsLayout.ResumeLayout(true);

    }

    private void UpdateDashboardLayout()
    {
        if (_dashboardMainPanel is null ||
            _dashboardScrollHost.ClientSize.Width <= 0 ||
            _dashboardScrollHost.ClientSize.Height <= 0)
        {
            return;
        }

        var contentWidth = _dashboardScrollHost.ClientSize.Width;
        var scale = _dashboardScrollHost.DeviceDpi / 96f;
        var minStackedHeight = (int)(600 * scale);
        var contentHeight = _heroCardsStacked
            ? Math.Max(minStackedHeight, _dashboardScrollHost.ClientSize.Height)
            : _dashboardScrollHost.ClientSize.Height;
        _dashboardMainPanel.Size = new Size(contentWidth, contentHeight);
        if (_dashboardMainPanel is TableLayoutPanel layout && layout.RowStyles.Count == 2)
        {
            var heroHeight = (int)((_heroCardsStacked ? 270 : 140) * scale);
            var kpisHeight = Math.Max(0, contentHeight - heroHeight);
            layout.RowStyles[0].SizeType = SizeType.Absolute;
            layout.RowStyles[0].Height = heroHeight;
            layout.RowStyles[1].SizeType = SizeType.Absolute;
            layout.RowStyles[1].Height = kpisHeight;
        }

        _dashboardScrollHost.AutoScrollMinSize = new Size(contentWidth, contentHeight);
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

        _summaryBalance.AutoSize = false;
        _summaryBalance.Dock = DockStyle.Fill;
        _summaryBalance.TextAlign = ContentAlignment.MiddleLeft;

        var chartArea = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Palette.Surface,
            Margin = new Padding(0)
        };
        chartArea.Controls.Add(_chart);
        chartArea.Controls.Add(_chartEmpty);

        var chartLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Palette.Surface,
            Margin = new Padding(0)
        };
        chartLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        chartLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        chartLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        chartLayout.Controls.Add(Ui.Header("Últimos 6 meses", _summaryFigures), 0, 0);
        chartLayout.Controls.Add(_summaryBalance, 0, 1);
        chartLayout.Controls.Add(chartArea, 0, 2);
        chartCard.Controls.Add(chartLayout);

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
            Padding = new Padding(12, 6, 12, 5),
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

    private sealed class DashboardScrollPanel : Panel
    {
        private const int ScrollBarsBoth = 3;
        private const int WmSize = 0x0005;
        private const int WmNcPaint = 0x0085;
        private const int WmStyleChanged = 0x007D;
        private const int WmVScroll = 0x0115;
        private const int WmHScroll = 0x0114;

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            HideScrollBars();
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            HideScrollBars();
        }

        protected override void WndProc(ref Message message)
        {
            base.WndProc(ref message);

            if (message.Msg is WmSize or WmNcPaint or WmStyleChanged or WmVScroll or WmHScroll)
            {
                HideScrollBars();
            }
        }

        private void HideScrollBars()
        {
            if (IsHandleCreated)
            {
                ShowScrollBar(Handle, ScrollBarsBoth, false);
            }
        }

        [DllImport("user32.dll", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ShowScrollBar(
            IntPtr hWnd,
            int wBar,
            [MarshalAs(UnmanagedType.Bool)] bool show);
    }
}
