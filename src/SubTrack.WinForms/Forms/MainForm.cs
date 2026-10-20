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

        Controls.Add(_contentPanel);
        Controls.Add(navigationHost);

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
