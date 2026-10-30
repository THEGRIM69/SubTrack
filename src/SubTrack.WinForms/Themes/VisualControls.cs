using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Globalization;

namespace SubTrack.WinForms.Themes;

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

        int diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
        var arc = new Rectangle(bounds.X, bounds.Y, diameter, diameter);

        path.AddArc(arc, 180, 90);
        arc.X = bounds.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = bounds.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = bounds.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();

        return path;
    }
}

internal class Card : Panel
{
    private int _radius = 16;
    private Color _fillColor = AppTheme.Surface;
    private Color _borderColor = AppTheme.Border;
    private int _borderWidth = 1;

    public Card()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);

        BackColor = _fillColor;
        Padding = new Padding(20);
        Margin = Padding.Empty;
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color FillColor
    {
        get => _fillColor;
        set
        {
            _fillColor = value;
            BackColor = value;
            Invalidate();
        }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color BorderColor
    {
        get => _borderColor;
        set
        {
            _borderColor = value;
            Invalidate();
        }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ShowBorder { get; set; } = true;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int BorderWidth
    {
        get => _borderWidth;
        set
        {
            _borderWidth = Math.Max(0, value);
            Invalidate();
        }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Radius
    {
        get => _radius;
        set
        {
            if (_radius == value)
            {
                return;
            }

            _radius = Math.Max(0, value);
            UpdateRegion();
            Invalidate();
        }
    }

    public void Fill(Color color) => FillColor = color;

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        Rectangle bounds = ClientRectangle;
        bounds.Width--;
        bounds.Height--;

        using GraphicsPath path = Shapes.Rounded(bounds, Radius);
        using var brush = new SolidBrush(FillColor);
        e.Graphics.FillPath(brush, path);

        if (ShowBorder && BorderWidth > 0 && BorderColor != Color.Transparent)
        {
            using var pen = new Pen(BorderColor, BorderWidth);
            e.Graphics.DrawPath(pen, path);
        }
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        UpdateRegion();
    }

    private void UpdateRegion()
    {
        if (ClientSize.Width <= 0 || ClientSize.Height <= 0)
        {
            return;
        }

        Rectangle bounds = ClientRectangle;
        bounds.Width--;
        bounds.Height--;

        using GraphicsPath path = Shapes.Rounded(bounds, Radius);
        Region? oldRegion = Region;
        Region = new Region(path);
        oldRegion?.Dispose();
    }
}

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
        Width = Math.Max(MinimumSize.Width, TextRenderer.MeasureText(text, AppTheme.Font(9.5F, FontStyle.Bold)).Width + 38);
        Margin = new Padding(0, 0, 8, 0);
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Cursor = Cursors.Hand;
        Font = AppTheme.Font(9.5F, FontStyle.Bold);
        TabStop = true;
        UseVisualStyleBackColor = false;

        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);
    }

    [Browsable(false)]
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
        if (e.Button == MouseButtons.Left)
        {
            _pressed = true;
            Invalidate();
        }

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
        Graphics graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Parent?.BackColor ?? AppTheme.WindowBackground);

        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
        (Color background, Color foreground, Color border) = ResolveColors();

        using GraphicsPath path = Shapes.Rounded(bounds, 10);
        if (background != Color.Transparent)
        {
            using var brush = new SolidBrush(background);
            graphics.FillPath(brush, path);
        }

        if (border != Color.Transparent)
        {
            using var pen = new Pen(border);
            graphics.DrawPath(pen, path);
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
            Rectangle focusBounds = Rectangle.Inflate(bounds, -3, -3);
            ControlPaint.DrawFocusRectangle(graphics, focusBounds, foreground, background);
        }
    }

    private (Color Background, Color Foreground, Color Border) ResolveColors()
    {
        if (!Enabled)
        {
            return (Color.FromArgb(238, 241, 243), Color.FromArgb(160, 170, 178), Color.Transparent);
        }

        (Color background, Color foreground, Color border) = Kind switch
        {
            ButtonKind.Primary => (AppTheme.Primary, AppTheme.Ink, Color.Transparent),
            ButtonKind.Dark => (AppTheme.Ink, Color.White, Color.Transparent),
            ButtonKind.Danger => (AppTheme.DangerSoft, AppTheme.Danger, Color.Transparent),
            ButtonKind.Ghost => (Color.Transparent, AppTheme.TextSecondary, Color.Transparent),
            _ => (AppTheme.Surface, AppTheme.TextPrimary, AppTheme.BorderStrong)
        };

        if (_pressed)
        {
            return (Darken(background, 0.92F), foreground, border);
        }

        if (_hover)
        {
            background = Kind switch
            {
                ButtonKind.Primary => AppTheme.PrimaryDeep,
                ButtonKind.Dark => AppTheme.InkHover,
                ButtonKind.Danger => Color.FromArgb(247, 217, 217),
                ButtonKind.Ghost => AppTheme.NavigationHover,
                _ => Color.FromArgb(244, 247, 248)
            };
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

internal sealed class Pill : Label
{
    public Pill(string text, Color background, Color foreground)
    {
        AutoSize = false;
        Height = 24;
        Text = text;
        BackColor = background;
        ForeColor = foreground;
        Font = AppTheme.Font(8.25F, FontStyle.Bold);
        TextAlign = ContentAlignment.MiddleCenter;
        Padding = new Padding(4, 0, 4, 0);
        Width = TextRenderer.MeasureText(text, Font).Width + 22;
        Margin = new Padding(0, 0, 8, 0);
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);

        Region?.Dispose();
        Region = Width > 0 && Height > 0
            ? new Region(Shapes.Rounded(new Rectangle(0, 0, Width, Height), Height / 2))
            : null;
    }
}

internal sealed record ComboItem<T>(T Value, string Text)
{
    public override string ToString() => Text;
}

internal static class Combo
{
    public static ComboBox Create<T>(IReadOnlyList<(T Value, string Text)> options, T selected, int width = 170)
    {
        var combo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat,
            Width = width,
            Font = AppTheme.Font(9.5F),
            Margin = new Padding(0, 0, 10, 0)
        };

        foreach ((T value, string text) in options)
        {
            combo.Items.Add(new ComboItem<T>(value, text));
        }

        if (options.Count > 0)
        {
            int selectedIndex = 0;

            for (int index = 0; index < options.Count; index++)
            {
                if (EqualityComparer<T>.Default.Equals(options[index].Value, selected))
                {
                    selectedIndex = index;
                    break;
                }
            }

            combo.SelectedIndex = selectedIndex;
        }

        return combo;
    }

    public static T Value<T>(ComboBox combo) => ((ComboItem<T>)combo.SelectedItem!).Value;
}

internal sealed class Pager : FlowLayoutPanel
{
    private readonly Label _label = new()
    {
        AutoSize = true,
        Margin = new Padding(6, 9, 6, 0),
        ForeColor = AppTheme.TextSecondary,
        Font = AppTheme.Font(9F)
    };

    private readonly FlatButton _previous;
    private readonly FlatButton _next;
    private int _page = 1;
    private int _totalPages = 1;

    public Pager()
    {
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        WrapContents = false;
        Margin = Padding.Empty;

        _previous = CreateArrowButton("‹");
        _next = CreateArrowButton("›");
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
        _totalPages = Math.Max(totalPages, 1);
        _page = Math.Clamp(page, 1, _totalPages);
        _label.Text = $"Página {_page} de {_totalPages}";
        _previous.Enabled = _page > 1;
        _next.Enabled = _page < _totalPages;
    }

    private static FlatButton CreateArrowButton(string text) => new(text, ButtonKind.Ghost)
    {
        Width = 32,
        Height = 32,
        MinimumSize = new Size(32, 32),
        Margin = Padding.Empty,
        Font = AppTheme.Font(13F, FontStyle.Bold)
    };
}

internal sealed class MonthStepper : FlowLayoutPanel
{
    private static readonly CultureInfo Spanish = CultureInfo.GetCultureInfo("es-PE");
    private readonly Label _label = new()
    {
        AutoSize = false,
        Width = 150,
        Height = 34,
        TextAlign = ContentAlignment.MiddleCenter,
        Font = AppTheme.Font(10F, FontStyle.Bold),
        ForeColor = AppTheme.TextPrimary,
        Margin = Padding.Empty
    };

    public MonthStepper()
    {
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        WrapContents = false;
        Height = 40;
        Margin = new Padding(0, 0, 12, 0);

        FlatButton previous = CreateArrowButton("‹");
        FlatButton next = CreateArrowButton("›");
        previous.Click += (_, _) => Shift(-1);
        next.Click += (_, _) => Shift(1);

        Controls.Add(previous);
        Controls.Add(_label);
        Controls.Add(next);
        RefreshLabel();
    }

    public DateOnly Month { get; private set; } = new(DateTime.Today.Year, DateTime.Today.Month, 1);

    public event Action? MonthChanged;

    public void SetMonth(DateOnly month)
    {
        Month = new DateOnly(month.Year, month.Month, 1);
        RefreshLabel();
    }

    private void Shift(int months)
    {
        Month = DateOnly.FromDateTime(Month.ToDateTime(TimeOnly.MinValue).AddMonths(months));
        RefreshLabel();
        MonthChanged?.Invoke();
    }

    private void RefreshLabel()
    {
        string text = Month.ToString("MMMM yyyy", Spanish);
        _label.Text = text.Length == 0 ? text : char.ToUpper(text[0], Spanish) + text[1..];
    }

    private static FlatButton CreateArrowButton(string text) => new(text, ButtonKind.Secondary)
    {
        Width = 34,
        Height = 34,
        MinimumSize = new Size(34, 34),
        Margin = Padding.Empty,
        Font = AppTheme.Font(12F, FontStyle.Bold)
    };
}
