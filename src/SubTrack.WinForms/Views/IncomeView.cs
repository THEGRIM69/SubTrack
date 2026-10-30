using SubTrack.WinForms.Themes;

namespace SubTrack.WinForms.Views;

internal sealed class IncomeView : UserControl
{
    private readonly FlatButton _incomeTab = new("Ingresos", ButtonKind.Primary)
    {
        MinimumSize = new Size(108, 34),
        Size = new Size(108, 34),
        Margin = new Padding(0, 0, 4, 0)
    };
    private readonly FlatButton _templatesTab = new("Plantillas", ButtonKind.Ghost)
    {
        MinimumSize = new Size(108, 34),
        Size = new Size(108, 34),
        Margin = Padding.Empty
    };
    private readonly DataGridView _incomeGrid = TableGrid.Create(
        ("Previsto", 15F), ("Real", 15F), ("Concepto", 30F), ("Importe", 15F), ("Tipo", 12F), ("Estado", 13F));
    private readonly DataGridView _templatesGrid = TableGrid.Create(
        ("Concepto", 32F), ("Importe", 18F), ("Frecuencia", 18F), ("Día", 12F), ("Inicio", 10F), ("Estado", 10F));
    private readonly Label _sectionTitle;
    private readonly Label _emptyMessage;

    public IncomeView()
    {
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScroll = true;
        BackColor = AppTheme.WindowBackground;
        Dock = DockStyle.Fill;

        _sectionTitle = CreateText("Ingresos registrados", AppTheme.PanelTitleFont, AppTheme.TextPrimary);
        _emptyMessage = CreateText(
            "No hay ingresos disponibles: este destino no incluye modelos ni servicios de ingresos.",
            AppTheme.Font(8.5F),
            AppTheme.TextSecondary);

        _incomeTab.Click += (_, _) => SelectTab(templates: false);
        _templatesTab.Click += (_, _) => SelectTab(templates: true);
        _templatesGrid.Visible = false;

        var root = new TableLayoutPanel
        {
            BackColor = AppTheme.WindowBackground,
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            RowCount = 3
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.Controls.Add(CreateHeading(), 0, 0);
        root.Controls.Add(CreateTabs(), 0, 1);
        root.Controls.Add(CreateTableCard(), 0, 2);
        Controls.Add(root);
    }

    private static Control CreateHeading()
    {
        var heading = new TableLayoutPanel
        {
            BackColor = AppTheme.WindowBackground,
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            Margin = new Padding(32, 18, 32, 8),
            RowCount = 2
        };
        heading.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        heading.RowStyles.Add(new RowStyle(SizeType.Absolute, 31F));
        heading.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        heading.Controls.Add(CreateText("Ingresos", AppTheme.TitleFont, AppTheme.TextPrimary), 0, 0);
        heading.Controls.Add(CreateText("Ingresos previstos, confirmados y plantillas recurrentes.", AppTheme.SubtitleFont, AppTheme.TextSecondary), 0, 1);
        return heading;
    }

    private Control CreateTabs()
    {
        var host = new Panel
        {
            BackColor = AppTheme.WindowBackground,
            Dock = DockStyle.Fill,
            Margin = new Padding(32, 0, 32, 10)
        };
        var segment = new Card
        {
            BackColor = AppTheme.Surface,
            Dock = DockStyle.Left,
            Padding = new Padding(4),
            Radius = 10,
            Size = new Size(232, 44)
        };
        var tabs = new FlowLayoutPanel
        {
            BackColor = AppTheme.Surface,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            WrapContents = false
        };
        tabs.Controls.Add(_incomeTab);
        tabs.Controls.Add(_templatesTab);
        segment.Controls.Add(tabs);
        host.Controls.Add(segment);
        return host;
    }

    private Control CreateTableCard()
    {
        var card = new Card
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(32, 0, 32, 28),
            Padding = new Padding(18, 14, 18, 12)
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
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
        layout.Controls.Add(CreateSectionHeading(), 0, 0);
        layout.Controls.Add(_incomeGrid, 0, 1);
        layout.Controls.Add(_templatesGrid, 0, 1);
        layout.Controls.Add(_emptyMessage, 0, 2);
        card.Controls.Add(layout);
        return card;
    }

    private Control CreateSectionHeading()
    {
        var heading = new TableLayoutPanel
        {
            BackColor = AppTheme.Surface,
            ColumnCount = 2,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            RowCount = 1
        };
        heading.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        heading.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        heading.Controls.Add(_sectionTitle, 0, 0);
        heading.Controls.Add(new Pill("SIN DATOS", AppTheme.PrimarySoft, AppTheme.PrimaryDeep)
        {
            Anchor = AnchorStyles.Right,
            Margin = Padding.Empty
        }, 1, 0);
        return heading;
    }

    private void SelectTab(bool templates)
    {
        _incomeTab.Kind = templates ? ButtonKind.Ghost : ButtonKind.Primary;
        _templatesTab.Kind = templates ? ButtonKind.Primary : ButtonKind.Ghost;
        _incomeTab.Invalidate();
        _templatesTab.Invalidate();
        _incomeGrid.Visible = !templates;
        _templatesGrid.Visible = templates;
        _sectionTitle.Text = templates ? "Plantillas recurrentes" : "Ingresos registrados";
        _emptyMessage.Text = templates
            ? "No hay plantillas disponibles: este destino no incluye modelos ni servicios de ingresos."
            : "No hay ingresos disponibles: este destino no incluye modelos ni servicios de ingresos.";
    }

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
