using SubTrack.WinForms.Controls;
using SubTrack.WinForms.Themes;
using SubTrack.WinForms.Views;

namespace SubTrack.WinForms.Forms;

internal sealed class MainForm : Form
{
    private readonly Panel _contentPanel;
    private readonly BottomNavigationBar _navigationBar;
    private readonly IReadOnlyDictionary<string, UserControl> _views;

    public MainForm()
    {
        Text = "SubTrack";
        BackColor = AppTheme.WindowBackground;
        ClientSize = new Size(1180, 760);
        MinimumSize = new Size(840, 600);
        StartPosition = FormStartPosition.CenterScreen;

        _contentPanel = new Panel
        {
            BackColor = AppTheme.WindowBackground,
            Dock = DockStyle.Fill
        };

        _navigationBar = new BottomNavigationBar();
        _navigationBar.SectionSelected += (_, section) => ShowSection(section);

        var header = CreateHeader();

        var navigationHost = new TableLayoutPanel
        {
            BackColor = AppTheme.WindowBackground,
            ColumnCount = 3,
            Dock = DockStyle.Bottom,
            Height = 86,
            Margin = Padding.Empty,
            Padding = new Padding(12, 8, 12, 14),
            RowCount = 1
        };
        navigationHost.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        navigationHost.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        navigationHost.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        navigationHost.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        navigationHost.Controls.Add(_navigationBar, 1, 0);
        _navigationBar.Anchor = AnchorStyles.None;

        var appLayout = new TableLayoutPanel
        {
            BackColor = AppTheme.WindowBackground,
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            RowCount = 3
        };
        appLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        appLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 82F));
        appLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        appLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 86F));
        appLayout.Controls.Add(header, 0, 0);
        appLayout.Controls.Add(_contentPanel, 0, 1);
        appLayout.Controls.Add(navigationHost, 0, 2);

        Controls.Add(appLayout);

        _views = new Dictionary<string, UserControl>
        {
            ["Dashboard"] = new DashboardView(),
            ["Suscripciones"] = new SubscriptionsView(),
            ["Cobros"] = new ChargesView(),
            ["Pagos"] = new PaymentsView(),
            ["Gastos"] = new ExpensesView(),
            ["Ingresos"] = new IncomeView()
        };

        ShowSection("Dashboard");
    }

    private static Control CreateHeader()
    {
        var header = new Panel
        {
            BackColor = AppTheme.Surface,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = new Padding(32, 16, 32, 15)
        };
        header.Paint += (_, e) =>
        {
            using var pen = new Pen(AppTheme.Border);
            e.Graphics.DrawLine(pen, 0, header.ClientSize.Height - 1, header.ClientSize.Width, header.ClientSize.Height - 1);
        };

        var layout = new TableLayoutPanel
        {
            BackColor = AppTheme.Surface,
            ColumnCount = 4,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            RowCount = 1
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 320F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var brand = new Label
        {
            AccessibleName = "Nombre de la aplicación",
            AutoSize = true,
            Font = AppTheme.BrandFont,
            ForeColor = AppTheme.TextPrimary,
            Margin = Padding.Empty,
            Text = "SubTrack 1.0",
            TextAlign = ContentAlignment.MiddleLeft
        };
        brand.Anchor = AnchorStyles.Left;

        var searchHost = new Panel
        {
            AccessibleName = "Buscar",
            BackColor = AppTheme.WindowBackground,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 2, 12, 2),
            Padding = new Padding(14, 0, 14, 0)
        };
        searchHost.Paint += (_, e) =>
        {
            using var pen = new Pen(AppTheme.Border);
            e.Graphics.DrawRectangle(pen, 0, 0, searchHost.ClientSize.Width - 1, searchHost.ClientSize.Height - 1);
        };

        var searchText = new Label
        {
            AutoEllipsis = true,
            BackColor = AppTheme.WindowBackground,
            Dock = DockStyle.Fill,
            Font = AppTheme.SearchFont,
            ForeColor = AppTheme.TextSecondary,
            Text = "⌕  Buscar...",
            TextAlign = ContentAlignment.MiddleLeft
        };
        searchHost.Controls.Add(searchText);

        var settingsButton = new Button
        {
            AccessibleName = "Configuración",
            AccessibleDescription = "Abrir configuración",
            BackColor = AppTheme.Surface,
            Cursor = Cursors.Hand,
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI Symbol", 16F, FontStyle.Regular),
            ForeColor = AppTheme.TextPrimary,
            Margin = new Padding(8, 2, 0, 2),
            TabStop = true,
            Text = "⚙",
            UseVisualStyleBackColor = false
        };
        settingsButton.FlatAppearance.BorderColor = AppTheme.Border;
        settingsButton.FlatAppearance.BorderSize = 1;
        settingsButton.FlatAppearance.MouseOverBackColor = AppTheme.PrimarySoft;

        layout.Controls.Add(brand, 0, 0);
        layout.Controls.Add(searchHost, 2, 0);
        layout.Controls.Add(settingsButton, 3, 0);
        header.Controls.Add(layout);

        return header;
    }

    private void ShowSection(string section)
    {
        if (!_views.TryGetValue(section, out UserControl? view))
        {
            return;
        }

        _contentPanel.SuspendLayout();
        _contentPanel.Controls.Clear();
        _contentPanel.Controls.Add(view);
        view.Dock = DockStyle.Fill;
        _contentPanel.ResumeLayout();
        _navigationBar.SetActiveSection(section);
    }
}
