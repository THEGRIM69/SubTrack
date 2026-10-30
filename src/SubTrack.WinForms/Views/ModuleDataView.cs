using SubTrack.WinForms.Themes;

namespace SubTrack.WinForms.Views;

internal class ModuleDataView : UserControl
{
    public ModuleDataView(
        string title,
        string subtitle,
        string sectionTitle,
        string unavailableMessage,
        params (string Header, float Weight)[] columns)
    {
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScroll = true;
        BackColor = AppTheme.WindowBackground;
        Dock = DockStyle.Fill;

        var root = new TableLayoutPanel
        {
            BackColor = AppTheme.WindowBackground,
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            RowCount = 3
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.Controls.Add(CreateHeading(title, subtitle), 0, 0);
        root.Controls.Add(CreateNotice(), 0, 1);
        root.Controls.Add(CreateTableCard(sectionTitle, unavailableMessage, columns), 0, 2);
        Controls.Add(root);
    }

    private static Control CreateHeading(string title, string subtitle)
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
        heading.Controls.Add(CreateText(title, AppTheme.TitleFont, AppTheme.TextPrimary), 0, 0);
        heading.Controls.Add(CreateText(subtitle, AppTheme.SubtitleFont, AppTheme.TextSecondary), 0, 1);
        return heading;
    }

    private static Control CreateNotice()
    {
        var notice = new Panel
        {
            BackColor = AppTheme.PrimarySoft,
            Dock = DockStyle.Fill,
            Margin = new Padding(32, 0, 32, 12),
            Padding = new Padding(12, 0, 12, 0)
        };
        notice.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Font = AppTheme.Font(8.5F),
            ForeColor = AppTheme.PrimaryDeep,
            Text = "Módulo visual preparado; no hay servicios de datos conectados en esta rama.",
            TextAlign = ContentAlignment.MiddleLeft
        });
        return notice;
    }

    private static Control CreateTableCard(
        string title,
        string unavailableMessage,
        (string Header, float Weight)[] columns)
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
        layout.Controls.Add(CreateSectionHeading(title), 0, 0);
        layout.Controls.Add(TableGrid.Create(columns), 0, 1);
        layout.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Font = AppTheme.Font(8.5F),
            ForeColor = AppTheme.TextSecondary,
            Margin = Padding.Empty,
            Text = unavailableMessage,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 2);
        card.Controls.Add(layout);
        return card;
    }

    private static Control CreateSectionHeading(string title)
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
        heading.Controls.Add(CreateText(title, AppTheme.PanelTitleFont, AppTheme.TextPrimary), 0, 0);
        heading.Controls.Add(new Pill("SIN DATOS", AppTheme.PrimarySoft, AppTheme.PrimaryDeep)
        {
            Anchor = AnchorStyles.Right,
            Margin = Padding.Empty
        }, 1, 0);
        return heading;
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