using System.Globalization;
using SubTrack.Core.Abstractions;
using SubTrack.Infrastructure;
using SubTrack.Infrastructure.Persistence;
using SubTrack.WinForms.Theme;
using SubTrack.WinForms.Views;

namespace SubTrack.WinForms;

/// <summary>
/// Ventana principal: navegación lateral y host de vistas. Consume exclusivamente
/// las interfaces de Core (F0-T03); aquí no hay SQL ni cálculos financieros.
/// </summary>
public sealed class MainForm : Form
{
    private static readonly CultureInfo Spanish = CultureInfo.GetCultureInfo("es-PE");

    private readonly SubTrackServices _services;
    private readonly SubTrackDatabaseOptions _options;
    private readonly Panel _content = new();
    private readonly Label _title = new();
    private readonly Label _subtitle = new();
    private readonly FlowLayoutPanel _navHost;
    private readonly Card _toast;
    private readonly Label _toastText;
    private readonly System.Windows.Forms.Timer _toastTimer = new() { Interval = 4200 };
    private readonly List<(NavButton Button, Func<ViewContext, Control> Factory)> _navigation = new();
    private readonly Dictionary<int, Control> _views = new();
    private int _current = -1;

    public MainForm(SubTrackServices services, SubTrackDatabaseOptions options)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _options = options ?? throw new ArgumentNullException(nameof(options));

        Text = "SubTrack 1.0";
        StartPosition = FormStartPosition.CenterScreen;
        WindowState = FormWindowState.Maximized;
        MinimumSize = new Size(1000, 640);
        Size = new Size(1320, 800);
        BackColor = Palette.Canvas;
        Font = Palette.Font(9.5f);
        DoubleBuffered = true;
        KeyPreview = true;
        AutoScaleMode = AutoScaleMode.None;

        var sidebar = new TableLayoutPanel
        {
            Dock = DockStyle.Left,
            Width = 244,
            BackColor = Palette.Ink,
            ColumnCount = 1,
            RowCount = 3,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };

        sidebar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 118));
        sidebar.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 94));

        _navHost = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = false,
            BackColor = Palette.Ink,
            Padding = new Padding(12, 8, 12, 8),
            Margin = new Padding(0)
        };

        sidebar.Controls.Add(BuildBrand(), 0, 0);
        sidebar.Controls.Add(_navHost, 0, 1);
        sidebar.Controls.Add(BuildFooter(), 0, 2);

        var main = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Palette.Canvas
        };

        _content.Dock = DockStyle.Fill;
        _content.BackColor = Palette.Canvas;
        _content.Padding = new Padding(28, 6, 28, 24);

        var header = BuildHeader();

        main.Controls.Add(_content);
        main.Controls.Add(header);

        Controls.Add(main);
        Controls.Add(sidebar);

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
        main.Controls.Add(_toast);
        main.Resize += (_, _) => PlaceToast();

        _toastTimer.Tick += (_, _) =>
        {
            _toastTimer.Stop();
            _toast.Visible = false;
        };

        AddNavigation("◫", "Resumen", ctx => new DashboardView(ctx));
        AddNavigation("◈", "Suscripciones", ctx => new SubscriptionsView(ctx));
        AddNavigation("◷", "Cobros y pagos", ctx => new ChargesView(ctx));
        AddNavigation("▽", "Gastos", ctx => new ExpensesView(ctx));
        AddNavigation("△", "Ingresos", ctx => new IncomesView(ctx));
        AddNavigation("⚙", "Configuración", ctx => new SettingsView(ctx));

        Shown += async (_, _) => await ShowSectionAsync(0);
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
            _ = ShowSectionAsync(_current);
            return true;
        }

        if (keyData >= (Keys.Control | Keys.D1) && keyData <= (Keys.Control | Keys.D6))
        {
            var index = (int)keyData - (int)(Keys.Control | Keys.D1);
            _ = ShowSectionAsync(index);
            return true;
        }

        return base.ProcessCmdKey(ref message, keyData);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _toastTimer.Dispose();
        base.OnFormClosed(e);
    }

    private void AddNavigation(string glyph, string text, Func<ViewContext, Control> factory)
    {
        var button = new NavButton(glyph, text)
        {
            Width = 220,
            Margin = new Padding(0, 0, 0, 4)
        };

        var index = _navigation.Count;
        button.Click += async (_, _) => await ShowSectionAsync(index);

        _navHost.Controls.Add(button);
        _navigation.Add((button, factory));
    }

    private async Task ShowSectionAsync(int index)
    {
        if (index < 0 || index >= _navigation.Count)
        {
            return;
        }

        if (_current == index)
        {
            if (_views.TryGetValue(index, out var open) && open is IAppView openView)
            {
                await openView.RefreshAsync();
            }

            return;
        }

        _current = index;

        for (var position = 0; position < _navigation.Count; position++)
        {
            _navigation[position].Button.Selected = position == index;
        }

        if (!_views.TryGetValue(index, out var view))
        {
            var context = new ViewContext(_services, _options, Notify);
            view = _navigation[index].Factory(context);
            view.Dock = DockStyle.Fill;
            _views[index] = view;
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
        if (_toast.Parent is not { } parent)
        {
            return;
        }

        _toast.Location = new Point(
            Math.Max(16, parent.ClientSize.Width - _toast.Width - 30),
            Math.Max(16, parent.ClientSize.Height - _toast.Height - 28));
    }

    private TableLayoutPanel BuildHeader()
    {
        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 96,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Palette.Canvas,
            Padding = new Padding(28, 18, 28, 6),
            Margin = new Padding(0)
        };

        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var titles = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Palette.Canvas,
            Margin = new Padding(0)
        };

        titles.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        titles.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _title.Dock = DockStyle.Fill;
        _title.Font = Palette.Font(21f, FontStyle.Bold);
        _title.ForeColor = Palette.Text;
        _title.TextAlign = ContentAlignment.MiddleLeft;
        _title.Text = "Resumen";

        _subtitle.Dock = DockStyle.Fill;
        _subtitle.Font = Palette.Font(9.5f);
        _subtitle.ForeColor = Palette.Muted;
        _subtitle.TextAlign = ContentAlignment.TopLeft;

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
            Margin = new Padding(0, 4, 0, 0)
        };

        header.Controls.Add(titles, 0, 0);
        header.Controls.Add(date, 1, 0);

        header.Paint += (_, args) =>
        {
            using var pen = new Pen(Palette.Border);
            args.Graphics.DrawLine(pen, 0, header.Height - 1, header.Width, header.Height - 1);
        };

        return header;
    }

    private static Panel BuildBrand()
    {
        var brand = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Palette.Ink,
            Margin = new Padding(0)
        };

        var logo = new Label
        {
            Text = "SubTrack",
            AutoSize = true,
            Font = Palette.Font(17f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Palette.Ink,
            Location = new Point(26, 34)
        };

        var accent = new Panel
        {
            Size = new Size(22, 3),
            BackColor = Palette.Primary,
            Location = new Point(28, 70)
        };

        var tagline = new Label
        {
            Text = "FINANZAS PERSONALES",
            AutoSize = true,
            Font = Palette.Font(7f, FontStyle.Bold),
            ForeColor = Color.FromArgb(118, 130, 140),
            BackColor = Palette.Ink,
            Location = new Point(28, 80)
        };

        brand.Controls.Add(tagline);
        brand.Controls.Add(accent);
        brand.Controls.Add(logo);

        return brand;
    }

    private static Panel BuildFooter()
    {
        var footer = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Palette.Ink,
            Margin = new Padding(0)
        };

        var dot = new Label
        {
            Text = "●",
            AutoSize = true,
            ForeColor = Palette.Primary,
            BackColor = Palette.Ink,
            Font = Palette.Font(8f),
            Location = new Point(26, 30)
        };

        var state = new Label
        {
            Text = "Datos en este equipo",
            AutoSize = true,
            ForeColor = Color.FromArgb(184, 194, 202),
            BackColor = Palette.Ink,
            Font = Palette.Font(9f),
            Location = new Point(44, 27)
        };

        var engine = new Label
        {
            Text = "SQLite · modo local",
            AutoSize = true,
            ForeColor = Color.FromArgb(110, 122, 132),
            BackColor = Palette.Ink,
            Font = Palette.Font(8f),
            Location = new Point(44, 48)
        };

        footer.Controls.Add(engine);
        footer.Controls.Add(state);
        footer.Controls.Add(dot);

        return footer;
    }
}
