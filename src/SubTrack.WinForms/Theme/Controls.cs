using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using SubTrack.Core.Domain.Enums;

namespace SubTrack.WinForms.Theme;

internal enum ButtonKind
{
    Primary,
    Secondary,
    Ghost,
    Danger,
    Dark
}

internal static class Shapes
{
    public static GraphicsPath Rounded(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();

        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return path;
        }

        if (radius <= 0)
        {
            path.AddRectangle(bounds);
            return path;
        }

        var diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));

        path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();

        return path;
    }
}

/// <summary>Tarjeta con esquinas redondeadas: la unidad visual base de la app.</summary>
internal class Card : Panel
{
    private int _radius = 16;

    public Card()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);

        BackColor = Palette.Surface;
        Padding = new Padding(20);
        Margin = new Padding(0);
    }

    public Color FillColor { get; private set; } = Palette.Surface;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color BorderColor { get; set; } = Palette.Border;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ShowBorder { get; set; } = true;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Radius
    {
        get => _radius;
        set
        {
            _radius = value;
            Invalidate();
        }
    }

    public void Fill(Color color)
    {
        FillColor = color;
        BackColor = color;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Parent?.BackColor ?? Palette.Canvas);

        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);

        using var path = Shapes.Rounded(bounds, Radius);
        using var fill = new SolidBrush(FillColor);
        graphics.FillPath(fill, path);

        if (ShowBorder)
        {
            using var pen = new Pen(BorderColor);
            graphics.DrawPath(pen, path);
        }
    }
}

/// <summary>Botón plano con hover, sin bordes nativos.</summary>
internal sealed class FlatButton : Button
{
    private bool _hover;
    private bool _pressed;

    public FlatButton(string text, ButtonKind kind = ButtonKind.Secondary)
    {
        Kind = kind;
        Text = text;
        AutoSize = false;
        Height = 40;
        MinimumSize = new Size(96, 40);
        Width = Math.Max(MinimumSize.Width, TextRenderer.MeasureText(text, Palette.Font(9.5f, FontStyle.Bold)).Width + 38);
        Margin = new Padding(0, 0, 8, 0);
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Cursor = Cursors.Hand;
        Font = Palette.Font(9.5f, FontStyle.Bold);
        TabStop = true;

        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ButtonKind Kind { get; set; }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hover = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hover = false;
        _pressed = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        _pressed = true;
        Invalidate();
        base.OnMouseDown(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        _pressed = false;
        Invalidate();
        base.OnMouseUp(e);
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        Invalidate();
        base.OnEnabledChanged(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Parent?.BackColor ?? Palette.Canvas);

        var (background, foreground, border) = ResolveColors();

        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);

        using (var path = Shapes.Rounded(bounds, 10))
        {
            using var brush = new SolidBrush(background);
            graphics.FillPath(brush, path);

            if (border != Color.Transparent)
            {
                using var pen = new Pen(border);
                graphics.DrawPath(pen, path);
            }
        }

        TextRenderer.DrawText(
            graphics,
            Text,
            Font,
            bounds,
            foreground,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

        if (Focused && ShowFocusCues)
        {
            var focus = Rectangle.Inflate(bounds, -3, -3);
            ControlPaint.DrawFocusRectangle(graphics, focus, foreground, background);
        }
    }

    private (Color Background, Color Foreground, Color Border) ResolveColors()
    {
        if (!Enabled)
        {
            return (Color.FromArgb(238, 241, 243), Color.FromArgb(160, 170, 178), Color.Transparent);
        }

        var (background, foreground, border) = Kind switch
        {
            ButtonKind.Primary => (Palette.Primary, Palette.Ink, Color.Transparent),
            ButtonKind.Dark => (Palette.Ink, Color.White, Color.Transparent),
            ButtonKind.Danger => (Palette.DangerSoft, Palette.Danger, Color.Transparent),
            ButtonKind.Ghost => (Color.Transparent, Palette.Muted, Color.Transparent),
            _ => (Palette.Surface, Palette.Text, Palette.BorderStrong)
        };

        if (_pressed)
        {
            return (Darken(background, 0.92f), foreground, border);
        }

        if (_hover)
        {
            return (Kind switch
            {
                ButtonKind.Primary => Palette.PrimaryDeep,
                ButtonKind.Dark => Palette.InkHover,
                ButtonKind.Danger => Color.FromArgb(247, 217, 217),
                ButtonKind.Ghost => Color.FromArgb(237, 241, 243),
                _ => Color.FromArgb(244, 247, 248)
            }, foreground, border);
        }

        return (background, foreground, border);
    }

    private static Color Darken(Color color, float factor)
    {
        if (color == Color.Transparent)
        {
            return Color.FromArgb(230, 234, 236);
        }

        return Color.FromArgb(
            color.A,
            (int)(color.R * factor),
            (int)(color.G * factor),
            (int)(color.B * factor));
    }
}

/// <summary>Etiqueta tipo pastilla para estados.</summary>
internal sealed class Pill : Label
{
    public Pill(string text, Color background, Color foreground)
    {
        AutoSize = false;
        Height = 24;
        Text = text;
        BackColor = background;
        ForeColor = foreground;
        Font = Palette.Font(8.25f, FontStyle.Bold);
        TextAlign = ContentAlignment.MiddleCenter;
        Padding = new Padding(4, 0, 4, 0);
        Width = TextRenderer.MeasureText(text, Font).Width + 22;
        Margin = new Padding(0, 0, 8, 0);

        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        Region = new Region(Shapes.Rounded(new Rectangle(0, 0, Width, Height), Height / 2));
    }
}

/// <summary>Ítem del menú lateral, dibujado a mano.</summary>
internal sealed class NavButton : Control
{
    private bool _hover;

    public NavButton(string glyph, string text)
    {
        Glyph = glyph;
        Text = text;
        Height = 46;
        Margin = new Padding(0, 0, 0, 4);
        Cursor = Cursors.Hand;
        Font = Palette.Font(10f);
        BackColor = Palette.Ink;
        TabStop = true;
        AccessibleRole = AccessibleRole.PushButton;
        AccessibleName = text;

        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.Selectable,
            true);
    }

    public string Glyph { get; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Selected { get; set; }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hover = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hover = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnGotFocus(EventArgs e)
    {
        Invalidate();
        base.OnGotFocus(e);
    }

    protected override void OnLostFocus(EventArgs e)
    {
        Invalidate();
        base.OnLostFocus(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode is Keys.Enter or Keys.Space)
        {
            OnClick(EventArgs.Empty);
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Palette.Ink);

        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);

        if (Selected || _hover)
        {
            using var path = Shapes.Rounded(bounds, 12);
            using var brush = new SolidBrush(Selected ? Palette.InkHover : Color.FromArgb(18, 24, 28));
            graphics.FillPath(brush, path);
        }

        if (Selected)
        {
            using var accent = Shapes.Rounded(new Rectangle(0, 12, 3, Height - 24), 2);
            using var brush = new SolidBrush(Palette.Primary);
            graphics.FillPath(brush, accent);
        }

        if (Focused)
        {
            using var focusPath = Shapes.Rounded(new Rectangle(2, 2, Width - 5, Height - 5), 10);
            using var focusPen = new Pen(Color.FromArgb(120, Palette.Primary));
            graphics.DrawPath(focusPen, focusPath);
        }

        var foreground = Selected ? Palette.Primary : Color.FromArgb(154, 166, 176);

        TextRenderer.DrawText(
            graphics,
            Glyph,
            Palette.Font(12f),
            new Rectangle(16, 0, 26, Height),
            foreground,
            TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);

        TextRenderer.DrawText(
            graphics,
            Text,
            Selected ? Palette.Font(10f, FontStyle.Bold) : Font,
            new Rectangle(48, 0, Width - 56, Height),
            foreground,
            TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }
}

internal sealed class Pager : FlowLayoutPanel
{
    private readonly Label _label = new()
    {
        AutoSize = true,
        Margin = new Padding(6, 9, 6, 0),
        ForeColor = Palette.Muted,
        Font = Palette.Font(9f)
    };

    private readonly FlatButton _previous;
    private readonly FlatButton _next;

    public Pager()
    {
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        WrapContents = false;
        Margin = new Padding(0);

        _previous = new FlatButton("‹", ButtonKind.Ghost)
        {
            Width = 32,
            Height = 32,
            MinimumSize = new Size(32, 32),
            Margin = new Padding(0)
        };
        _previous.Font = Palette.Font(13f, FontStyle.Bold);

        _next = new FlatButton("›", ButtonKind.Ghost)
        {
            Width = 32,
            Height = 32,
            MinimumSize = new Size(32, 32),
            Margin = new Padding(0)
        };
        _next.Font = Palette.Font(13f, FontStyle.Bold);

        _previous.Click += (_, _) => Previous?.Invoke();
        _next.Click += (_, _) => Next?.Invoke();

        Controls.Add(_previous);
        Controls.Add(_label);
        Controls.Add(_next);

        SetPage(1, 1);
    }

    public event Action? Previous;

    public event Action? Next;

    public void SetPage(int page, int totalPages)
    {
        var total = Math.Max(totalPages, 1);

        _label.Text = $"Página {page} de {total}";
        _previous.Enabled = page > 1;
        _next.Enabled = page < total;
    }
}

/// <summary>Selector de mes con flechas, sin DateTimePicker nativo.</summary>
internal sealed class MonthStepper : FlowLayoutPanel
{
    private static readonly CultureInfo Spanish = CultureInfo.GetCultureInfo("es-PE");

    private readonly Label _label = new()
    {
        AutoSize = false,
        Width = 150,
        Height = 34,
        TextAlign = ContentAlignment.MiddleCenter,
        Font = Palette.Font(10f, FontStyle.Bold),
        ForeColor = Palette.Text,
        Margin = new Padding(0)
    };

    private readonly FlatButton _previous;
    private readonly FlatButton _next;

    public MonthStepper()
    {
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        WrapContents = false;
        Height = 40;
        Margin = new Padding(0, 0, 12, 0);

        _previous = new FlatButton("‹", ButtonKind.Secondary)
        {
            Width = 34,
            Height = 34,
            MinimumSize = new Size(34, 34),
            Margin = new Padding(0)
        };
        _previous.Font = Palette.Font(12f, FontStyle.Bold);

        _next = new FlatButton("›", ButtonKind.Secondary)
        {
            Width = 34,
            Height = 34,
            MinimumSize = new Size(34, 34),
            Margin = new Padding(0)
        };
        _next.Font = Palette.Font(12f, FontStyle.Bold);

        _previous.Click += (_, _) => Shift(-1);
        _next.Click += (_, _) => Shift(1);

        Controls.Add(_previous);
        Controls.Add(_label);
        Controls.Add(_next);

        RefreshLabel();
    }

    public DateOnly Month { get; private set; } =
        new(DateTime.Today.Year, DateTime.Today.Month, 1);

    public event Action? MonthChanged;

    public void SetMonth(DateOnly month)
    {
        Month = new DateOnly(month.Year, month.Month, 1);
        RefreshLabel();
    }

    private void Shift(int months)
    {
        var moved = Month.ToDateTime(TimeOnly.MinValue).AddMonths(months);
        Month = DateOnly.FromDateTime(moved);
        RefreshLabel();
        MonthChanged?.Invoke();
    }

    private void RefreshLabel()
    {
        var text = Month.ToString("MMMM yyyy", Spanish);
        _label.Text = text.Length == 0 ? text : char.ToUpper(text[0], Spanish) + text[1..];
    }
}

/// <summary>Gráfico de barras agrupadas (ingresos vs. egresos) por mes.</summary>
internal sealed class BarsChart : Control
{
    public sealed record Bar(string Label, long IncomeCents, long ExpenseCents);

    private IReadOnlyList<Bar> _bars = Array.Empty<Bar>();

    public BarsChart()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);

        BackColor = Palette.Surface;
    }

    public void SetData(IReadOnlyList<Bar> bars)
    {
        _bars = bars;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        graphics.Clear(BackColor);

        DrawLegend(graphics);

        var area = new Rectangle(8, 34, Width - 16, Height - 34 - 26);

        if (_bars.Count == 0 || area.Width <= 20 || area.Height <= 20)
        {
            return;
        }

        var maximum = _bars.Max(bar => Math.Max(bar.IncomeCents, bar.ExpenseCents));

        if (maximum <= 0)
        {
            maximum = 1;
        }

        using (var gridPen = new Pen(Color.FromArgb(240, 243, 245)))
        {
            for (var line = 0; line <= 3; line++)
            {
                var y = area.Bottom - (int)(area.Height * line / 3.0);
                graphics.DrawLine(gridPen, area.Left, y, area.Right, y);
            }
        }

        var slot = area.Width / (float)_bars.Count;
        var barWidth = Math.Min(18f, slot / 3.4f);
        var labelFont = Palette.Font(8f);

        for (var index = 0; index < _bars.Count; index++)
        {
            var bar = _bars[index];
            var center = area.Left + (slot * index) + (slot / 2f);
            var incomeHeight = (float)(area.Height * (bar.IncomeCents / (double)maximum));
            var expenseHeight = (float)(area.Height * (bar.ExpenseCents / (double)maximum));

            DrawBar(graphics, center - barWidth - 2, area.Bottom, barWidth, incomeHeight, Palette.Primary);
            DrawBar(graphics, center + 2, area.Bottom, barWidth, expenseHeight, Color.FromArgb(214, 221, 226));

            var size = graphics.MeasureString(bar.Label, labelFont);

            using var labelBrush = new SolidBrush(Palette.Muted);
            graphics.DrawString(
                bar.Label,
                labelFont,
                labelBrush,
                center - (size.Width / 2f),
                area.Bottom + 6);
        }
    }

    private void DrawLegend(Graphics graphics)
    {
        var font = Palette.Font(8.25f);
        var x = 8f;

        DrawLegendItem(graphics, font, ref x, "Ingresos", Palette.Primary);
        DrawLegendItem(graphics, font, ref x, "Egresos", Color.FromArgb(214, 221, 226));
    }

    private static void DrawLegendItem(Graphics graphics, Font font, ref float x, string label, Color color)
    {
        using var dot = new SolidBrush(color);
        graphics.FillEllipse(dot, x, 12, 8, 8);

        using var text = new SolidBrush(Palette.Muted);
        graphics.DrawString(label, font, text, x + 13, 8);

        x += 13 + graphics.MeasureString(label, font).Width + 18;
    }

    private static void DrawBar(Graphics graphics, float x, float bottom, float width, float height, Color color)
    {
        var displayHeight = Math.Max(height, 6f);
        var bounds = Rectangle.Round(new RectangleF(x, bottom - displayHeight, width, displayHeight));

        if (bounds.Width < 1 || bounds.Height < 1)
        {
            return;
        }

        var radius = Math.Min(4, bounds.Width / 2);

        using var path = Shapes.Rounded(bounds, radius);
        using var brush = new SolidBrush(color);
        graphics.FillPath(brush, path);
    }
}

internal sealed record ComboItem<T>(T Value, string Text)
{
    public override string ToString() => Text;
}

internal static class Combo
{
    public static ComboBox Create<T>(
        IReadOnlyList<(T Value, string Text)> options,
        T selected,
        int width = 170)
    {
        var combo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat,
            Width = width,
            Font = Palette.Font(9.5f),
            Margin = new Padding(0, 0, 10, 0)
        };

        foreach (var (value, text) in options)
        {
            combo.Items.Add(new ComboItem<T>(value, text));
        }

        var selectedIndex = 0;

        for (var index = 0; index < options.Count; index++)
        {
            if (EqualityComparer<T>.Default.Equals(options[index].Value, selected))
            {
                selectedIndex = index;
                break;
            }
        }

        combo.SelectedIndex = selectedIndex;

        return combo;
    }

    public static T Value<T>(ComboBox combo) => ((ComboItem<T>)combo.SelectedItem!).Value;
}

/// <summary>Fábrica de controles con el lenguaje visual de la app. Sin reglas de negocio.</summary>
internal static class Ui
{
    public static FlatButton Action(
        string text,
        Func<Task> action,
        ButtonKind kind = ButtonKind.Secondary)
    {
        var button = new FlatButton(text, kind);

        button.Click += async (_, _) =>
        {
            button.Enabled = false;

            try
            {
                await action();
            }
            catch (Exception exception)
            {
                MessageBox.Show(
                    $"Error inesperado:{Environment.NewLine}{exception.Message}",
                    "SubTrack",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                if (!button.IsDisposed)
                {
                    button.Enabled = true;
                }
            }
        };

        return button;
    }

    public static Control Toolbar(Control[] left, Control[] right)
    {
        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 0, 0, 14),
            BackColor = Palette.Canvas
        };

        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var leftFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };

        leftFlow.Controls.AddRange(left);

        var rightFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            FlowDirection = FlowDirection.RightToLeft,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };

        rightFlow.Controls.AddRange(right);

        table.Controls.Add(leftFlow, 0, 0);
        table.Controls.Add(rightFlow, 1, 0);

        return table;
    }

    public static Label Label(string text, float size = 9.5f, Color? color = null, FontStyle style = FontStyle.Regular) =>
        new()
        {
            Text = text,
            AutoSize = true,
            Font = Palette.Font(size, style),
            ForeColor = color ?? Palette.Text,
            Margin = new Padding(0)
        };

    public static Label Caption(string text) =>
        new()
        {
            Text = text.ToUpperInvariant(),
            AutoSize = true,
            Font = Palette.Font(7.75f, FontStyle.Bold),
            ForeColor = Palette.Muted,
            Margin = new Padding(0)
        };

    public static Label CardTitle(string text) =>
        new()
        {
            Text = text,
            Dock = DockStyle.Top,
            Height = 30,
            Font = Palette.Font(11f, FontStyle.Bold),
            ForeColor = Palette.Text,
            BackColor = Color.Transparent
        };

    public static TableLayoutPanel Header(string title, Control? right = null)
    {
        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 40,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Palette.Surface,
            Margin = new Padding(0)
        };

        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var titleLabel = new Label
        {
            Text = title,
            Dock = DockStyle.Fill,
            Font = Palette.Font(11f, FontStyle.Bold),
            ForeColor = Palette.Text,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0)
        };

        table.Controls.Add(titleLabel, 0, 0);

        if (right is not null)
        {
            right.Dock = DockStyle.Fill;
            right.Margin = new Padding(8, 0, 0, 0);
            table.Controls.Add(right, 1, 0);
        }

        return table;
    }

    public static FlatButton Menu(string text, params (string Text, Func<Task> Action)[] items)
    {
        var button = new FlatButton($"{text}  ▾", ButtonKind.Secondary);

        var menu = new ContextMenuStrip
        {
            BackColor = Palette.Surface,
            ForeColor = Palette.Text,
            Font = Palette.Font(9.5f),
            ShowImageMargin = false,
            Padding = new Padding(5),
            Renderer = new MenuRenderer()
        };

        foreach (var (label, action) in items)
        {
            var item = new ToolStripMenuItem(label)
            {
                Padding = new Padding(8, 6, 18, 6)
            };

            item.Click += async (_, _) =>
            {
                try
                {
                    await action();
                }
                catch (Exception exception)
                {
                    MessageBox.Show(
                        $"Error inesperado:{Environment.NewLine}{exception.Message}",
                        "SubTrack",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            };

            menu.Items.Add(item);
        }

        button.Click += (_, _) => menu.Show(button, new Point(0, button.Height + 4));

        return button;
    }

    public static T? Selected<T>(DataGridView grid) where T : class =>
        grid.SelectedRows.Count == 0 ? null : grid.SelectedRows[0].Tag as T;

    public static FlatButton Compact(FlatButton button)
    {
        button.Height = 34;
        button.MinimumSize = new Size(button.MinimumSize.Width, 34);
        button.Margin = new Padding(0, 0, 6, 0);
        return button;
    }

    public static Label Section(string text, string? hint = null)
    {
        var label = new Label
        {
            Text = text,
            Dock = DockStyle.Top,
            Height = hint is null ? 30 : 44,
            Font = Palette.Font(11f, FontStyle.Bold),
            ForeColor = Palette.Text
        };

        if (hint is not null)
        {
            label.Text = $"{text}\n{hint}";
        }

        return label;
    }

    public static SearchBox Search(string placeholder, int width = 220) => new(placeholder, width);

    public static DataGridView Grid(params (string Header, float Weight)[] columns)
    {
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            AllowUserToResizeColumns = false,
            AllowUserToOrderColumns = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            RowHeadersVisible = false,
            BackgroundColor = Palette.Surface,
            BorderStyle = BorderStyle.None,
            EnableHeadersVisualStyles = false,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            ColumnHeadersHeight = 42,
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            GridColor = Color.FromArgb(240, 243, 245),
            Font = Palette.Font(9.5f),
            ShowCellToolTips = true,
            ShowEditingIcon = false
        };

        grid.RowTemplate.Height = 44;

        grid.DefaultCellStyle.BackColor = Palette.Surface;
        grid.DefaultCellStyle.ForeColor = Palette.Text;
        grid.DefaultCellStyle.SelectionBackColor = Palette.PrimarySoft;
        grid.DefaultCellStyle.SelectionForeColor = Palette.Text;
        grid.DefaultCellStyle.Padding = new Padding(6, 0, 6, 0);
        grid.DefaultCellStyle.Font = Palette.Font(9.5f);

        grid.AlternatingRowsDefaultCellStyle.BackColor = Palette.Surface;
        grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = Palette.PrimarySoft;

        grid.ColumnHeadersDefaultCellStyle.BackColor = Palette.Surface;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Palette.Muted;
        grid.ColumnHeadersDefaultCellStyle.Font = Palette.Font(8.25f, FontStyle.Bold);
        grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(6, 0, 6, 0);
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Palette.Surface;
        grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Palette.Muted;

        foreach (var (header, weight) in columns)
        {
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = header,
                HeaderText = header,
                FillWeight = weight,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });
        }

        return grid;
    }

    public static void Align(DataGridView grid, params string[] headers)
    {
        foreach (var header in headers)
        {
            if (grid.Columns[header] is { } column)
            {
                column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
        }
    }

    public static void Tint(DataGridViewRow row, string column, Color color)
    {
        if (row.Cells[column] is { } cell)
        {
            cell.Style.ForeColor = color;
            cell.Style.Font = Palette.Font(9.5f, FontStyle.Bold);
        }
    }

    public static (string Text, Color Color) ExpenseBadge(ExpenseStatus status) => status switch
    {
        ExpenseStatus.Planned => ("● Previsto", Palette.Warning),
        ExpenseStatus.Confirmed => ("● Confirmado", Palette.PrimaryDeep),
        ExpenseStatus.Cancelled => ("● Anulado", Palette.Muted),
        _ => ("—", Palette.Muted)
    };

    public static (string Text, Color Color) IncomeBadge(IncomeStatus status) => status switch
    {
        IncomeStatus.Planned => ("● Previsto", Palette.Warning),
        IncomeStatus.Confirmed => ("● Confirmado", Palette.PrimaryDeep),
        IncomeStatus.Cancelled => ("● Anulado", Palette.Muted),
        _ => ("—", Palette.Muted)
    };

    public static (string Text, Color Color) PaymentBadge(PaymentStatus status) => status switch
    {
        PaymentStatus.Active => ("● Vigente", Palette.PrimaryDeep),
        PaymentStatus.Cancelled => ("● Anulado", Palette.Muted),
        _ => ("—", Palette.Muted)
    };

    public static Label Divider() => new()
    {
        Height = 1,
        Dock = DockStyle.Top,
        BackColor = Palette.Border,
        Margin = new Padding(0, 8, 0, 8)
    };

    public static Control Spacer(int height = 8) => new Panel
    {
        Height = height,
        Dock = DockStyle.Top,
        BackColor = Color.Transparent
    };
}

/// <summary>
/// Campo de búsqueda redondeado con lupa; el editor real queda accesible en <see cref="Editor"/>.
/// </summary>
internal sealed class SearchBox : Control
{
    private readonly TextBox _box;

    public SearchBox(string placeholder, int width)
    {
        Width = width;
        Height = 38;
        Margin = new Padding(0, 0, 10, 0);
        BackColor = Palette.Surface;
        Cursor = Cursors.IBeam;
        TabStop = false;
        Font = Palette.Font(9.5f);

        _box = new TextBox
        {
            BorderStyle = BorderStyle.None,
            BackColor = Palette.Surface,
            ForeColor = Palette.Text,
            Font = Font,
            Width = width - 52,
            Location = new Point(38, 9)
        };

        Native.Cue(_box, placeholder);
        _box.GotFocus += (_, _) => Invalidate();
        _box.LostFocus += (_, _) => Invalidate();
        Controls.Add(_box);

        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);
    }

    public TextBox Editor => _box;

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Parent?.BackColor ?? Palette.Canvas);

        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);

        using (var path = Shapes.Rounded(bounds, 10))
        using (var fill = new SolidBrush(Palette.Surface))
        using (var border = new Pen(_box.Focused ? Palette.Primary : Palette.Border, _box.Focused ? 1.6f : 1f))
        {
            graphics.FillPath(fill, path);
            graphics.DrawPath(border, path);
        }

        var center = Height / 2f;

        using var pen = new Pen(_box.Focused ? Palette.PrimaryDeep : Palette.Muted, 1.6f);
        graphics.DrawEllipse(pen, 14f, center - 7f, 11f, 11f);
        graphics.DrawLine(pen, 23.5f, center + 3.5f, 28f, center + 8f);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);

        if (_box is null)
        {
            return;
        }

        _box.Width = Math.Max(40, Width - 52);
        _box.Location = new Point(38, (Height - _box.Height) / 2 - 1);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        _box.Focus();
        base.OnMouseDown(e);
    }
}

internal sealed class MenuRenderer : ToolStripProfessionalRenderer
{
    public MenuRenderer()
        : base(new MenuColors())
    {
    }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        var bounds = new Rectangle(Point.Empty, e.Item.Size);

        using var brush = new SolidBrush(e.Item.Selected ? Palette.PrimarySoft : Palette.Surface);
        e.Graphics.FillRectangle(brush, bounds);
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = e.Item.Selected ? Palette.PrimaryDeep : Palette.Text;
        base.OnRenderItemText(e);
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
    }
}

internal sealed class MenuColors : ProfessionalColorTable
{
    public override Color ToolStripDropDownBackground => Palette.Surface;

    public override Color MenuBorder => Palette.Border;

    public override Color ImageMarginGradientBegin => Palette.Surface;

    public override Color ImageMarginGradientMiddle => Palette.Surface;

    public override Color ImageMarginGradientEnd => Palette.Surface;

    public override Color MenuItemSelected => Palette.PrimarySoft;

    public override Color MenuItemBorder => Palette.PrimarySoft;
}
