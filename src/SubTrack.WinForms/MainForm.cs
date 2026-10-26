using System.Globalization;
using SubTrack.Infrastructure;
using SubTrack.Infrastructure.Persistence;
using SubTrack.WinForms.Controls;
using SubTrack.WinForms.Theme;
using SubTrack.WinForms.Themes;
using SubTrack.WinForms.Views;

namespace SubTrack.WinForms;

/// <summary>
/// Ventana principal: navegación inferior centrada (GRIM) y host funcional de vistas (COSMO).
/// Consume exclusivamente las interfaces de Core (F0-T03); aquí no hay SQL ni cálculos financieros.
/// </summary>
public sealed class MainForm : Form
{
    private static readonly CultureInfo Spanish = CultureInfo.GetCultureInfo("es-PE");

    private readonly SubTrackServices _services;
    private readonly SubTrackDatabaseOptions _options;
    private readonly Panel _content = new();
    private readonly Label _title = new();
    private readonly Label _subtitle = new();
    private readonly BottomNavigationBar _navigationBar;
    private readonly Button _settingsButton = new();
    private readonly Card _toast;
    private readonly Label _toastText;
    private readonly System.Windows.Forms.Timer _toastTimer = new() { Interval = 4200 };
    private readonly Dictionary<string, (int Index, Func<ViewContext, Control> Factory)> _navigation = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Control> _views = new(StringComparer.OrdinalIgnoreCase);
    private string? _currentSection;

    public MainForm(SubTrackServices services, SubTrackDatabaseOptions options)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _options = options ?? throw new ArgumentNullException(nameof(options));

        Text = "SubTrack 1.0";
        StartPosition = FormStartPosition.CenterScreen;
        WindowState = FormWindowState.Normal;
        MinimumSize = new Size(840, 600);
        Size = new Size(1200, 780);
        BackColor = Palette.Canvas;
        Font = Palette.Font(9.5f);
        DoubleBuffered = true;
        KeyPreview = true;
        AutoScaleMode = AutoScaleMode.None;

        _navigationBar = new BottomNavigationBar();
        _navigationBar.SectionSelected += async (_, section) => await ShowSectionAsync(section);

        var navigationHost = new TableLayoutPanel
        {
            BackColor = Palette.Canvas,
            ColumnCount = 3,
            Dock = DockStyle.Fill,
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

        var header = BuildHeader();

        _content.Dock = DockStyle.Fill;
        _content.BackColor = Palette.Canvas;
        _content.Padding = new Padding(28, 12, 28, 12);

        var appLayout = new TableLayoutPanel
        {
            BackColor = Palette.Canvas,
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            RowCount = 3
        };
        appLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        appLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 84F));
        appLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        appLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 86F));
        appLayout.Controls.Add(header, 0, 0);
        appLayout.Controls.Add(_content, 0, 1);
        appLayout.Controls.Add(navigationHost, 0, 2);

        Controls.Add(appLayout);

        _toast = new Card
        {
            Radius = 12,
            Size = new Size(360, 54),
            Visible = false,
            Padding = new Padding(18, 0, 18, 0)
        };

        _toastText = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.White,
            Font = Palette.Font(9f),
            AutoEllipsis = true,
            BackColor = Color.Transparent
        };

        _toast.Controls.Add(_toastText);
        Controls.Add(_toast);
        _toast.BringToFront();
        Resize += (_, _) => PlaceToast();

        _toastTimer.Tick += (_, _) =>
        {
            _toastTimer.Stop();
            _toast.Visible = false;
        };

        RegisterNavigation("Dashboard", 0, ctx => new DashboardView(ctx));
        RegisterNavigation("Suscripciones", 1, ctx => new SubscriptionsView(ctx));
        RegisterNavigation("Cobros", 2, ctx => new ChargesView(ctx, ChargesViewMode.Charges));
        RegisterNavigation("Pagos", 3, ctx => new PaymentsView(ctx));
        RegisterNavigation("Gastos", 4, ctx => new ExpensesView(ctx));
        RegisterNavigation("Ingresos", 5, ctx => new IncomesView(ctx));
        RegisterNavigation("Configuración", 6, ctx => new SettingsView(ctx));

        Shown += async (_, _) => await ShowSectionAsync("Dashboard");
    }

    public void Notify(string message, bool error = false)
    {
        if (IsDisposed)
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(() => Notify(message, error));
            return;
        }

        _toast.Fill(error ? Palette.Danger : Palette.Ink);
        _toastText.Text = message;
        _toast.Visible = true;
        _toast.BringToFront();
        PlaceToast();

        _toastTimer.Stop();
        _toastTimer.Start();
    }

    protected override bool ProcessCmdKey(ref Message message, Keys keyData)
    {
        if (keyData == Keys.F5)
        {
            if (_currentSection != null)
            {
                _ = ShowSectionAsync(_currentSection);
            }
            return true;
        }

        if (keyData >= (Keys.Control | Keys.D1) && keyData <= (Keys.Control | Keys.D6))
        {
            var index = (int)keyData - (int)(Keys.Control | Keys.D1);
            var sections = new[] { "Dashboard", "Suscripciones", "Cobros", "Pagos", "Gastos", "Ingresos" };
            if (index >= 0 && index < sections.Length)
            {
                _ = ShowSectionAsync(sections[index]);
                return true;
            }
        }

        if (keyData is (Keys.Control | Keys.Oemcomma) or (Keys.Control | Keys.D7))
        {
            _ = ShowSectionAsync("Configuración");
            return true;
        }

        return base.ProcessCmdKey(ref message, keyData);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _toastTimer.Dispose();
        base.OnFormClosed(e);
    }

    private void RegisterNavigation(string section, int index, Func<ViewContext, Control> factory)
    {
        _navigation[section] = (index, factory);
    }

    private async Task ShowSectionAsync(string section)
    {
        if (!_navigation.TryGetValue(section, out var entry))
        {
            return;
        }

        if (string.Equals(_currentSection, section, StringComparison.OrdinalIgnoreCase))
        {
            if (_views.TryGetValue(section, out var open) && open is IAppView openView)
            {
                await openView.RefreshAsync();
            }

            return;
        }

        _currentSection = section;

        var isSettings = string.Equals(section, "Configuración", StringComparison.OrdinalIgnoreCase);
        _settingsButton.BackColor = isSettings ? Palette.PrimarySoft : Palette.Surface;
        _settingsButton.FlatAppearance.BorderColor = isSettings ? Palette.Primary : Palette.Border;

        _navigationBar.SetActiveSection(isSettings ? string.Empty : section);

        if (!_views.TryGetValue(section, out var view))
        {
            var context = new ViewContext(_services, _options, Notify);
            view = entry.Factory(context);
            view.Dock = DockStyle.Fill;
            _views[section] = view;
            _content.Controls.Add(view);
        }

        foreach (Control control in _content.Controls)
        {
            control.Visible = control == view;
        }

        if (view is IAppView appView)
        {
            _title.Text = appView.Title;
            _subtitle.Text = appView.Subtitle;

            await appView.RefreshAsync();
        }
    }

    private void PlaceToast()
    {
        _toast.Location = new Point(
            Math.Max(16, ClientSize.Width - _toast.Width - 30),
            Math.Max(16, ClientSize.Height - _toast.Height - 96));
    }

    private Control BuildHeader()
    {
        var header = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Palette.Surface,
            Margin = Padding.Empty,
            Padding = new Padding(28, 12, 28, 12)
        };

        header.Paint += (_, e) =>
        {
            using var pen = new Pen(Palette.Border);
            e.Graphics.DrawLine(pen, 0, header.ClientSize.Height - 1, header.ClientSize.Width, header.ClientSize.Height - 1);
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 5,
            RowCount = 1,
            BackColor = Palette.Surface,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));             // Col 0: Brand
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 28F));        // Col 1: Separator gap
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));        // Col 2: Section Titles
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));             // Col 3: Date
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50F));        // Col 4: Settings Button
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var brandPanel = new Panel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Palette.Surface,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            Anchor = AnchorStyles.Left
        };

        var brandTitle = new Label
        {
            AccessibleName = "Nombre de la aplicación",
            Text = "SubTrack 1.0",
            AutoSize = true,
            Font = AppTheme.BrandFont,
            ForeColor = Palette.Text,
            Location = new Point(0, 4),
            Margin = Padding.Empty
        };

        var brandTagline = new Label
        {
            Text = "FINANZAS PERSONALES",
            AutoSize = true,
            Font = Palette.Font(7f, FontStyle.Bold),
            ForeColor = Palette.Muted,
            Location = new Point(1, 32),
            Margin = Padding.Empty
        };

        brandPanel.Controls.Add(brandTitle);
        brandPanel.Controls.Add(brandTagline);

        var separator = new Panel
        {
            Width = 1,
            Dock = DockStyle.Fill,
            Margin = new Padding(14, 8, 14, 8),
            BackColor = Palette.Border
        };

        var titles = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Palette.Surface,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        titles.RowStyles.Add(new RowStyle(SizeType.Percent, 55F));
        titles.RowStyles.Add(new RowStyle(SizeType.Percent, 45F));

        _title.Dock = DockStyle.Fill;
        _title.Font = Palette.Font(16f, FontStyle.Bold);
        _title.ForeColor = Palette.Text;
        _title.TextAlign = ContentAlignment.MiddleLeft;
        _title.Text = "Dashboard";
        _title.AutoEllipsis = true;

        _subtitle.Dock = DockStyle.Fill;
        _subtitle.Font = Palette.Font(9f);
        _subtitle.ForeColor = Palette.Muted;
        _subtitle.TextAlign = ContentAlignment.TopLeft;
        _subtitle.AutoEllipsis = true;

        titles.Controls.Add(_title, 0, 0);
        titles.Controls.Add(_subtitle, 0, 1);

        var today = DateTime.Today.ToString("dddd, d 'de' MMMM", Spanish);
        today = char.ToUpper(today[0], Spanish) + today[1..];

        var date = new Label
        {
            Text = today,
            AutoSize = true,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight,
            Font = Palette.Font(9f),
            ForeColor = Palette.Muted,
            Margin = new Padding(0, 0, 16, 0)
        };

        _settingsButton.AccessibleName = "Configuración";
        _settingsButton.AccessibleDescription = "Abrir configuración";
        _settingsButton.AccessibleRole = AccessibleRole.PushButton;
        _settingsButton.BackColor = Palette.Surface;
        _settingsButton.Cursor = Cursors.Hand;
        _settingsButton.Dock = DockStyle.Fill;
        _settingsButton.FlatStyle = FlatStyle.Flat;
        _settingsButton.Font = new Font("Segoe UI Symbol", 15F, FontStyle.Regular);
        _settingsButton.ForeColor = Palette.Text;
        _settingsButton.Margin = new Padding(4, 8, 0, 8);
        _settingsButton.TabStop = true;
        _settingsButton.Text = "⚙";
        _settingsButton.UseVisualStyleBackColor = false;
        _settingsButton.FlatAppearance.BorderColor = Palette.Border;
        _settingsButton.FlatAppearance.BorderSize = 1;
        _settingsButton.FlatAppearance.MouseOverBackColor = Palette.PrimarySoft;

        _settingsButton.Click += async (_, _) => await ShowSectionAsync("Configuración");

        layout.Controls.Add(brandPanel, 0, 0);
        layout.Controls.Add(separator, 1, 0);
        layout.Controls.Add(titles, 2, 0);
        layout.Controls.Add(date, 3, 0);
        layout.Controls.Add(_settingsButton, 4, 0);

        header.Controls.Add(layout);
        return header;
    }
}
