using System.Drawing.Drawing2D;
using SubTrack.WinForms.Themes;

namespace SubTrack.WinForms.Controls;

internal sealed class BottomNavigationBar : UserControl
{
    private const int CornerRadius = 16;
    private readonly FlowLayoutPanel _itemsPanel;
    private readonly Dictionary<string, NavigationButton> _buttons = [];

    public event EventHandler<string>? SectionSelected;

    public BottomNavigationBar()
    {
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        BackColor = AppTheme.Surface;
        DoubleBuffered = true;
        Margin = Padding.Empty;
        Padding = new Padding(8, 5, 8, 5);

        _itemsPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = AppTheme.Surface,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            WrapContents = false
        };

        Controls.Add(_itemsPanel);

        AddItem("Dashboard", NavigationIcon.Dashboard, 102);
        AddItem("Suscripciones", NavigationIcon.Subscriptions, 124);
        AddItem("Cobros", NavigationIcon.Charges, 94);
        AddItem("Pagos", NavigationIcon.Payments, 94);
        AddItem("Gastos", NavigationIcon.Expenses, 94);
        AddItem("Ingresos", NavigationIcon.Income, 94);
    }

    public void SetActiveSection(string section)
    {
        foreach ((string name, NavigationButton button) in _buttons)
        {
            button.IsActive = name == section;
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using GraphicsPath path = CreateRoundedRectangle(ClientRectangle, CornerRadius);
        using var pen = new Pen(AppTheme.Border);
        e.Graphics.DrawPath(pen, path);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        Region? oldRegion = Region;
        using GraphicsPath path = CreateRoundedRectangle(ClientRectangle, CornerRadius);
        Region = new Region(path);
        oldRegion?.Dispose();
    }

    private void AddItem(string name, NavigationIcon icon, int width)
    {
        var button = new NavigationButton(icon)
        {
            AccessibleName = name,
            AccessibleRole = AccessibleRole.PushButton,
            Height = 54,
            Margin = new Padding(2, 0, 2, 0),
            TabStop = true,
            Text = name,
            Width = width
        };
        button.Click += (_, _) => SectionSelected?.Invoke(this, name);

        _buttons.Add(name, button);
        _itemsPanel.Controls.Add(button);
    }

    private static GraphicsPath CreateRoundedRectangle(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return path;
        }

        bounds.Width--;
        bounds.Height--;
        int diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
        var arc = new Rectangle(bounds.Location, new Size(diameter, diameter));

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

    private enum NavigationIcon
    {
        Dashboard,
        Subscriptions,
        Charges,
        Payments,
        Expenses,
        Income
    }

    private sealed class NavigationButton : Button
    {
        private const int ButtonRadius = 10;
        private readonly NavigationIcon _icon;
        private bool _isActive;
        private bool _isHovered;
        private bool _isPressed;

        public NavigationButton(NavigationIcon icon)
        {
            _icon = icon;
            BackColor = AppTheme.Surface;
            Cursor = Cursors.Hand;
            FlatStyle = FlatStyle.Flat;
            Font = AppTheme.NavigationFont;
            ForeColor = AppTheme.TextSecondary;
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.UserPaint, true);
            FlatAppearance.BorderSize = 0;
            UseVisualStyleBackColor = false;
        }

        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive == value)
                {
                    return;
                }

                _isActive = value;
                AccessibleDescription = value ? "Módulo activo" : null;
                Invalidate();
            }
        }

        protected override void OnEnter(EventArgs e)
        {
            base.OnEnter(e);
            Invalidate();
        }

        protected override void OnLeave(EventArgs e)
        {
            base.OnLeave(e);
            _isPressed = false;
            Invalidate();
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _isHovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _isHovered = false;
            _isPressed = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            base.OnMouseDown(mevent);
            if (mevent.Button == MouseButtons.Left)
            {
                _isPressed = true;
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs mevent)
        {
            base.OnMouseUp(mevent);
            _isPressed = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            pevent.Graphics.Clear(AppTheme.Surface);

            Rectangle bounds = ClientRectangle;
            bounds.Inflate(-1, -1);
            using GraphicsPath backgroundPath = CreateRoundedRectangle(bounds, ButtonRadius);
            Color background = _isPressed
                ? AppTheme.PrimaryPressed
                : _isActive
                    ? AppTheme.PrimarySoft
                    : _isHovered || Focused
                        ? AppTheme.NavigationHover
                        : AppTheme.Surface;
            using (var brush = new SolidBrush(background))
            {
                pevent.Graphics.FillPath(brush, backgroundPath);
            }

            Color foreground = _isActive ? AppTheme.Primary : AppTheme.TextSecondary;
            using var iconPen = new Pen(foreground, 1.8F)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };
            DrawIcon(pevent.Graphics, iconPen, new Rectangle((Width - 22) / 2, 6, 22, 22));

            Font labelFont = _isActive ? AppTheme.NavigationActiveFont : AppTheme.NavigationFont;
            TextRenderer.DrawText(
                pevent.Graphics,
                Text,
                labelFont,
                new Rectangle(3, 31, Width - 6, Height - 33),
                foreground,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            if (Focused && ShowFocusCues)
            {
                var focusBounds = Rectangle.Inflate(bounds, -3, -3);
                ControlPaint.DrawFocusRectangle(pevent.Graphics, focusBounds, foreground, background);
            }
        }

        private void DrawIcon(Graphics graphics, Pen pen, Rectangle bounds)
        {
            float left = bounds.Left + 2;
            float top = bounds.Top + 2;
            float right = bounds.Right - 2;
            float bottom = bounds.Bottom - 2;
            float centerX = bounds.Left + bounds.Width / 2F;
            float centerY = bounds.Top + bounds.Height / 2F;

            switch (_icon)
            {
                case NavigationIcon.Dashboard:
                    graphics.DrawRectangle(pen, left, top, 7, 7);
                    graphics.DrawRectangle(pen, right - 7, top, 7, 7);
                    graphics.DrawRectangle(pen, left, bottom - 7, 7, 7);
                    graphics.DrawRectangle(pen, right - 7, bottom - 7, 7, 7);
                    break;
                case NavigationIcon.Subscriptions:
                    graphics.DrawRectangle(pen, left, top + 1, right - left, bottom - top - 2);
                    graphics.DrawLine(pen, left + 4, top + 6, right - 4, top + 6);
                    graphics.DrawLine(pen, left + 4, top + 11, right - 7, top + 11);
                    break;
                case NavigationIcon.Charges:
                    graphics.DrawEllipse(pen, left, top, right - left, bottom - top);
                    graphics.DrawLine(pen, centerX, top + 4, centerX, centerY);
                    graphics.DrawLine(pen, centerX, centerY, right - 4, centerY + 3);
                    break;
                case NavigationIcon.Payments:
                    graphics.DrawEllipse(pen, left, top, right - left, bottom - top);
                    graphics.DrawLines(pen, [
                        new PointF(left + 4, centerY),
                        new PointF(centerX - 1, bottom - 4),
                        new PointF(right - 3, top + 5)]);
                    break;
                case NavigationIcon.Expenses:
                    graphics.DrawLine(pen, centerX, top + 2, centerX, bottom - 3);
                    graphics.DrawLine(pen, centerX, bottom - 3, left + 4, centerY + 2);
                    graphics.DrawLine(pen, centerX, bottom - 3, right - 4, centerY + 2);
                    break;
                case NavigationIcon.Income:
                    graphics.DrawLine(pen, centerX, bottom - 2, centerX, top + 3);
                    graphics.DrawLine(pen, centerX, top + 3, left + 4, centerY - 2);
                    graphics.DrawLine(pen, centerX, top + 3, right - 4, centerY - 2);
                    break;
            }
        }
    }
}
