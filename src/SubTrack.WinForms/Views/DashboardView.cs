using SubTrack.WinForms.Themes;

namespace SubTrack.WinForms.Views;

internal sealed class DashboardView : UserControl
{
    private readonly MonthStepper _month = new() { Enabled = false };

    public DashboardView()
    {
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScroll = true;
        BackColor = AppTheme.WindowBackground;
        Dock = DockStyle.Fill;

        var content = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = AppTheme.WindowBackground,
            ColumnCount = 1,
            Dock = DockStyle.Top,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            RowCount = 5
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 72F));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 158F));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 300F));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 270F));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 206F));
        content.Controls.Add(CreateHeading(), 0, 0);
        content.Controls.Add(CreateSummary(), 0, 1);
        content.Controls.Add(CreateChargeSections(), 0, 2);
        content.Controls.Add(CreateActivitySection(), 0, 3);
        content.Controls.Add(CreateChartSection(), 0, 4);
        Controls.Add(content);
    }

    private Control CreateHeading()
    {
        var heading = new TableLayoutPanel
        {
            BackColor = AppTheme.WindowBackground,
            ColumnCount = 2,
            Dock = DockStyle.Fill,
            Margin = new Padding(32, 18, 32, 8),
            RowCount = 1
        };
        heading.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        heading.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var titles = new TableLayoutPanel
        {
            BackColor = AppTheme.WindowBackground,
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            RowCount = 2
        };
        titles.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        titles.RowStyles.Add(new RowStyle(SizeType.Absolute, 31F));
        titles.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        titles.Controls.Add(CreateText("Resumen", AppTheme.TitleFont, AppTheme.TextPrimary), 0, 0);
        titles.Controls.Add(CreateText("Tu foto financiera del mes, sin ruido.", AppTheme.SubtitleFont, AppTheme.TextSecondary), 0, 1);

        _month.AccessibleDescription = "Selector mensual deshabilitado: no hay servicios de dashboard conectados";
        _month.Margin = new Padding(16, 6, 0, 0);
        heading.Controls.Add(titles, 0, 0);
        heading.Controls.Add(_month, 1, 0);
        return heading;
    }

    private static Control CreateSummary()
    {
        var card = new Card
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(32, 0, 32, 16),
            Padding = new Padding(22, 16, 22, 16)
        };
        var layout = new TableLayoutPanel
        {
            BackColor = AppTheme.Surface,
            ColumnCount = 4,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            RowCount = 2
        };
        for (int index = 0; index < 4; index++)
        {
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        }
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.Controls.Add(CreateText("Resumen financiero", AppTheme.PanelTitleFont, AppTheme.TextPrimary), 0, 0);
        layout.SetColumnSpan(layout.GetControlFromPosition(0, 0)!, 4);
        layout.Controls.Add(CreateMetric("Saldo actual"), 0, 1);
        layout.Controls.Add(CreateMetric("Ahorro proyectado"), 1, 1);
        layout.Controls.Add(CreateMetric("Ingresos del mes"), 2, 1);
        layout.Controls.Add(CreateMetric("Gastos del mes"), 3, 1);
        card.Controls.Add(layout);
        return card;
    }

    private static Control CreateMetric(string caption)
    {
        var metric = new TableLayoutPanel
        {
            BackColor = AppTheme.WindowBackground,
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            Margin = new Padding(4, 4, 8, 0),
            Padding = new Padding(12, 8, 8, 8),
            RowCount = 2
        };
        metric.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        metric.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        metric.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        metric.Controls.Add(CreateText(caption, AppTheme.CaptionFont, AppTheme.TextSecondary), 0, 0);
        metric.Controls.Add(CreateText("Sin datos", AppTheme.ItemBoldFont, AppTheme.TextPrimary), 0, 1);
        return metric;
    }

    private static Control CreateChargeSections()
    {
        var columns = new TableLayoutPanel
        {
            BackColor = AppTheme.WindowBackground,
            ColumnCount = 2,
            Dock = DockStyle.Fill,
            Margin = new Padding(32, 0, 32, 16),
            RowCount = 1
        };
        columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        columns.Controls.Add(CreateTableSection(
            "Próximos cobros",
            "Sin cobros disponibles en este destino.",
            ("Fecha", 24F), ("Suscripción", 52F), ("Importe", 24F)), 0, 0);
        columns.Controls.Add(CreateTableSection(
            "Cobros vencidos",
            "Sin cobros vencidos disponibles.",
            ("Fecha", 24F), ("Suscripción", 52F), ("Importe", 24F)), 1, 0);
        return columns;
    }

    private static Control CreateActivitySection() => CreateTableSection(
        "Actividad reciente",
        "Aún no hay movimientos conectados para mostrar.",
        ("Fecha", 20F), ("Movimiento", 54F), ("Importe", 26F),
        margin: new Padding(32, 0, 32, 16));

    private static Control CreateChartSection()
    {
        var card = new Card
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(32, 0, 32, 28),
            Padding = new Padding(20, 14, 20, 14)
        };
        var layout = new TableLayoutPanel
        {
            BackColor = AppTheme.Surface,
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            RowCount = 2
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.Controls.Add(CreateText("Ingresos y gastos", AppTheme.PanelTitleFont, AppTheme.TextPrimary), 0, 0);
        layout.Controls.Add(CreateEmptyMessage("El gráfico estará disponible cuando exista una fuente de datos financieros."), 0, 1);
        card.Controls.Add(layout);
        return card;
    }

    private static Control CreateTableSection(
        string title,
        string emptyMessage,
        (string Header, float Weight) first,
        (string Header, float Weight) second,
        (string Header, float Weight) third,
        Padding? margin = null)
    {
        var card = new Card
        {
            Dock = DockStyle.Fill,
            Margin = margin ?? new Padding(0, 0, 10, 0),
            Padding = new Padding(16, 12, 16, 12)
        };
        var layout = new TableLayoutPanel
        {
            BackColor = AppTheme.Surface,
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            RowCount = 3
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
        layout.Controls.Add(CreateText(title, AppTheme.PanelTitleFont, AppTheme.TextPrimary), 0, 0);
        layout.Controls.Add(TableGrid.Create(first, second, third), 0, 1);
        layout.Controls.Add(CreateEmptyMessage(emptyMessage), 0, 2);
        card.Controls.Add(layout);
        return card;
    }

    private static Control CreateEmptyMessage(string text) => new Label
    {
        Dock = DockStyle.Fill,
        Font = AppTheme.Font(8.5F),
        ForeColor = AppTheme.TextSecondary,
        Margin = Padding.Empty,
        Text = text,
        TextAlign = ContentAlignment.MiddleLeft
    };

    private static Label CreateText(string text, Font font, Color color) => new()
    {
        AutoEllipsis = true,
        Dock = DockStyle.Fill,
        Font = font,
        ForeColor = color,
        Margin = Padding.Empty,
        Text = text,
        TextAlign = ContentAlignment.MiddleLeft
    };
}