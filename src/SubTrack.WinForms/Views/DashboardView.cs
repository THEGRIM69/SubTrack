using System.Drawing.Drawing2D;
using SubTrack.WinForms.Themes;

namespace SubTrack.WinForms.Views;

internal sealed class DashboardView : UserControl
{
    private const int MinimumTwoColumnCardWidth = 520;
    private const float DateColumnWidth = 94F;
    private const float ChargeAmountColumnWidth = 104F;
    private const float ActivityAmountColumnWidth = 116F;
    private const float StatusColumnWidth = 112F;
    private readonly Panel _heading;
    private readonly DashboardCard _upcomingCharges;
    private readonly DashboardCard _recentActivity;

    public DashboardView()
    {
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScroll = true;
        BackColor = AppTheme.WindowBackground;
        Dock = DockStyle.Fill;

        _heading = CreateHeading();
        _upcomingCharges = CreateUpcomingChargesCard();
        _recentActivity = CreateRecentActivityCard();

        Controls.Add(_heading);
        Controls.Add(_upcomingCharges);
        Controls.Add(_recentActivity);

        PerformLayout();
    }

    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);

        // WinForms can request layout while the base Control constructor is still
        // applying properties, before this view has created its child controls.
        if (_heading is null || _upcomingCharges is null || _recentActivity is null)
        {
            return;
        }

        int outerHorizontal = ScaleLogical(40);
        int top = ScaleLogical(30);
        int headingHeight = ScaleLogical(58);
        int headingGap = ScaleLogical(22);
        int cardGap = ScaleLogical(18);
        int cardHeight = ScaleLogical(334);
        int bottom = ScaleLogical(30);
        int scrollAllowance = VerticalScroll.Visible ? SystemInformation.VerticalScrollBarWidth : 0;
        int availableWidth = Math.Max(ScaleLogical(360), ClientSize.Width - (outerHorizontal * 2) - scrollAllowance);

        _heading.SetBounds(outerHorizontal, top, availableWidth, headingHeight);
        int cardsTop = top + headingHeight + headingGap;
        int twoColumnCardWidth = (availableWidth - cardGap) / 2;

        if (twoColumnCardWidth >= ScaleLogical(MinimumTwoColumnCardWidth))
        {
            _upcomingCharges.SetBounds(outerHorizontal, cardsTop, twoColumnCardWidth, cardHeight);
            _recentActivity.SetBounds(
                outerHorizontal + twoColumnCardWidth + cardGap,
                cardsTop,
                twoColumnCardWidth,
                cardHeight);
            SetAutoScrollMinSize(new Size(0, cardsTop + cardHeight + bottom));
        }
        else
        {
            _upcomingCharges.SetBounds(outerHorizontal, cardsTop, availableWidth, cardHeight);
            _recentActivity.SetBounds(outerHorizontal, cardsTop + cardHeight + cardGap, availableWidth, cardHeight);
            SetAutoScrollMinSize(new Size(0, cardsTop + (cardHeight * 2) + cardGap + bottom));
        }
    }

    private int ScaleLogical(int value) => value * DeviceDpi / 96;

    private void SetAutoScrollMinSize(Size requiredSize)
    {
        if (AutoScrollMinSize != requiredSize)
        {
            AutoScrollMinSize = requiredSize;
        }
    }

    private static Panel CreateHeading()
    {
        var heading = new Panel
        {
            BackColor = AppTheme.WindowBackground,
            Margin = Padding.Empty
        };

        var title = new Label
        {
            AutoSize = true,
            Font = AppTheme.TitleFont,
            ForeColor = AppTheme.TextPrimary,
            Location = Point.Empty,
            Text = "Dashboard"
        };

        var subtitle = new Label
        {
            AutoSize = true,
            Font = AppTheme.SubtitleFont,
            ForeColor = AppTheme.TextSecondary,
            Location = new Point(2, 38),
            Text = "Resumen de próximos movimientos y actividad reciente"
        };

        var demoBadge = new Label
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            AutoSize = false,
            BackColor = AppTheme.PrimarySoft,
            Font = AppTheme.CaptionBoldFont,
            ForeColor = AppTheme.Primary,
            Height = 25,
            Text = "DATOS DE DEMOSTRACIÓN",
            TextAlign = ContentAlignment.MiddleCenter,
            Top = 5,
            Width = 190
        };
        demoBadge.Left = heading.Width - demoBadge.Width;
        heading.Resize += (_, _) => demoBadge.Left = heading.ClientSize.Width - demoBadge.Width;

        heading.Controls.Add(title);
        heading.Controls.Add(subtitle);
        heading.Controls.Add(demoBadge);
        return heading;
    }

    private static DashboardCard CreateUpcomingChargesCard()
    {
        var card = new DashboardCard("Próximos cargos", DashboardIcon.Calendar);
        card.AddItem(CreateUpcomingChargeRow(
            DashboardIcon.Play,
            "StreamWave",
            "28 oct 2026",
            "$12.99",
            "Programado",
            AppTheme.Primary));
        card.AddItem(CreateUpcomingChargeRow(
            DashboardIcon.Cloud,
            "CloudBox Pro",
            "02 nov 2026",
            "$8.50",
            "Próximo",
            AppTheme.Warning));
        card.AddItem(CreateUpcomingChargeRow(
            DashboardIcon.Music,
            "MusicFlow",
            "06 nov 2026",
            "$6.99",
            "Programado",
            AppTheme.Primary));
        return card;
    }

    private static DashboardCard CreateRecentActivityCard()
    {
        var card = new DashboardCard("Actividad reciente", DashboardIcon.Activity);
        card.AddItem(CreateActivityRow(
            DashboardIcon.Check,
            "Pago registrado",
            "StreamWave · Demostración",
            "20 oct 2026",
            "− $12.99",
            AppTheme.TextPrimary));
        card.AddItem(CreateActivityRow(
            DashboardIcon.ArrowUp,
            "Ingreso registrado",
            "Ingreso mensual · Demostración",
            "18 oct 2026",
            "+ $850.00",
            AppTheme.Success));
        card.AddItem(CreateActivityRow(
            DashboardIcon.ArrowDown,
            "Gasto registrado",
            "Servicio de internet · Demostración",
            "15 oct 2026",
            "− $34.00",
            AppTheme.TextPrimary));
        return card;
    }

    private static Control CreateUpcomingChargeRow(
        DashboardIcon icon,
        string service,
        string date,
        string amount,
        string status,
        Color statusColor)
    {
        var row = CreateRowLayout();
        row.ColumnCount = 5;
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 46F));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, DateColumnWidth));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, ChargeAmountColumnWidth));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, StatusColumnWidth));

        row.Controls.Add(new DashboardGlyph(icon), 0, 0);
        row.Controls.Add(CreateTextStack(service, "Servicio · Demostración"), 1, 0);
        row.Controls.Add(CreateCenteredLabel(date, AppTheme.TextSecondary, AppTheme.CaptionFont), 2, 0);
        row.Controls.Add(CreatePrimaryAlignedLabel(amount, AppTheme.TextPrimary), 3, 0);
        row.Controls.Add(CreateStatusLabel(status, statusColor), 4, 0);
        return row;
    }

    private static Control CreateActivityRow(
        DashboardIcon icon,
        string movement,
        string description,
        string date,
        string amount,
        Color amountColor)
    {
        var row = CreateRowLayout();
        row.ColumnCount = 4;
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 46F));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, DateColumnWidth));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, ActivityAmountColumnWidth));

        row.Controls.Add(new DashboardGlyph(icon), 0, 0);
        row.Controls.Add(CreateTextStack(movement, description), 1, 0);
        row.Controls.Add(CreateCenteredLabel(date, AppTheme.TextSecondary, AppTheme.CaptionFont), 2, 0);
        row.Controls.Add(CreatePrimaryAlignedLabel(amount, amountColor), 3, 0);
        return row;
    }

    private static TableLayoutPanel CreateRowLayout() => new()
    {
        BackColor = AppTheme.Surface,
        Dock = DockStyle.Fill,
        Margin = Padding.Empty,
        Padding = new Padding(10, 5, 10, 5),
        RowCount = 1
    };

    private static Control CreateTextStack(string primary, string secondary)
    {
        var stack = new TableLayoutPanel
        {
            BackColor = AppTheme.Surface,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            RowCount = 2
        };
        stack.RowStyles.Add(new RowStyle(SizeType.Percent, 52F));
        stack.RowStyles.Add(new RowStyle(SizeType.Percent, 48F));

        stack.Controls.Add(new Label
        {
            AutoEllipsis = true,
            Dock = DockStyle.Fill,
            Font = AppTheme.ItemBoldFont,
            ForeColor = AppTheme.TextPrimary,
            Margin = Padding.Empty,
            Text = primary,
            TextAlign = ContentAlignment.BottomLeft
        }, 0, 0);
        stack.Controls.Add(new Label
        {
            AutoEllipsis = true,
            Dock = DockStyle.Fill,
            Font = AppTheme.CaptionFont,
            ForeColor = AppTheme.TextSecondary,
            Margin = Padding.Empty,
            Text = secondary,
            TextAlign = ContentAlignment.TopLeft
        }, 0, 1);
        return stack;
    }

    private static Label CreateCenteredLabel(string text, Color color, Font font) => new()
    {
        AutoEllipsis = true,
        Dock = DockStyle.Fill,
        Font = font,
        ForeColor = color,
        Margin = Padding.Empty,
        Text = text,
        TextAlign = ContentAlignment.MiddleRight
    };

    private static Control CreatePrimaryAlignedLabel(string text, Color color)
    {
        var stack = new TableLayoutPanel
        {
            BackColor = AppTheme.Surface,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            RowCount = 2
        };
        stack.RowStyles.Add(new RowStyle(SizeType.Percent, 52F));
        stack.RowStyles.Add(new RowStyle(SizeType.Percent, 48F));
        stack.Controls.Add(new Label
        {
            AutoEllipsis = false,
            Dock = DockStyle.Fill,
            Font = AppTheme.ItemBoldFont,
            ForeColor = color,
            Margin = Padding.Empty,
            Text = text,
            TextAlign = ContentAlignment.BottomRight
        }, 0, 0);
        return stack;
    }

    private static Control CreateStatusLabel(string status, Color statusColor)
    {
        var host = new TableLayoutPanel
        {
            BackColor = AppTheme.Surface,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            RowCount = 3
        };
        host.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        host.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        host.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

        var label = new Label
        {
            Anchor = AnchorStyles.None,
            AutoSize = true,
            BackColor = AppTheme.PrimarySoft,
            Font = AppTheme.CaptionBoldFont,
            ForeColor = statusColor,
            Margin = Padding.Empty,
            Padding = new Padding(7, 4, 7, 4),
            Text = status,
            TextAlign = ContentAlignment.MiddleCenter
        };
        host.Controls.Add(label, 0, 1);
        return host;
    }

    private enum DashboardIcon
    {
        Calendar,
        Activity,
        Play,
        Cloud,
        Music,
        Check,
        ArrowUp,
        ArrowDown
    }

    private sealed class DashboardCard : Panel
    {
        private readonly TableLayoutPanel _layout;
        private int _nextRow = 1;

        public DashboardCard(string title, DashboardIcon icon)
        {
            BackColor = AppTheme.Surface;
            DoubleBuffered = true;
            Margin = Padding.Empty;
            Padding = new Padding(1);

            _layout = new TableLayoutPanel
            {
                BackColor = AppTheme.Surface,
                ColumnCount = 1,
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                RowCount = 4
            };
            _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 66F));
            _layout.RowStyles.Add(new RowStyle(SizeType.Percent, 33.333F));
            _layout.RowStyles.Add(new RowStyle(SizeType.Percent, 33.333F));
            _layout.RowStyles.Add(new RowStyle(SizeType.Percent, 33.334F));

            var header = new Panel
            {
                BackColor = AppTheme.Surface,
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                Padding = new Padding(18, 13, 18, 9)
            };
            var glyph = new DashboardGlyph(icon)
            {
                Dock = DockStyle.Left,
                Margin = Padding.Empty,
                Width = 38
            };
            var label = new Label
            {
                Dock = DockStyle.Fill,
                Font = AppTheme.PanelTitleFont,
                ForeColor = AppTheme.TextPrimary,
                Margin = Padding.Empty,
                Padding = new Padding(8, 0, 0, 0),
                Text = title,
                TextAlign = ContentAlignment.MiddleLeft
            };
            header.Controls.Add(label);
            header.Controls.Add(glyph);
            _layout.Controls.Add(header, 0, 0);
            Controls.Add(_layout);
        }

        public void AddItem(Control item)
        {
            item.Margin = new Padding(0, 1, 0, 0);
            _layout.Controls.Add(item, 0, _nextRow++);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle bounds = ClientRectangle;
            bounds.Width--;
            bounds.Height--;
            using GraphicsPath path = CreateRoundedPath(bounds, 14);
            using var pen = new Pen(AppTheme.Border);
            e.Graphics.DrawPath(pen, path);
        }

        protected override void OnResize(EventArgs eventargs)
        {
            base.OnResize(eventargs);
            Rectangle bounds = ClientRectangle;
            bounds.Width--;
            bounds.Height--;
            using GraphicsPath path = CreateRoundedPath(bounds, 14);
            Region? previous = Region;
            Region = new Region(path);
            previous?.Dispose();
        }

        private static GraphicsPath CreateRoundedPath(Rectangle bounds, int radius)
        {
            var path = new GraphicsPath();
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                return path;
            }

            int diameter = radius * 2;
            var arc = new Rectangle(bounds.Left, bounds.Top, diameter, diameter);
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

    private sealed class DashboardGlyph : Control
    {
        private readonly DashboardIcon _icon;

        public DashboardGlyph(DashboardIcon icon)
        {
            _icon = icon;
            BackColor = AppTheme.Surface;
            Dock = DockStyle.Fill;
            Margin = Padding.Empty;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            float size = Math.Min(22F, Math.Min(Width - 8F, Height - 8F));
            if (size <= 4)
            {
                return;
            }

            var bounds = new RectangleF((Width - size) / 2F, (Height - size) / 2F, size, size);
            using var softBrush = new SolidBrush(AppTheme.PrimarySoft);
            e.Graphics.FillEllipse(softBrush, bounds);

            RectangleF icon = RectangleF.Inflate(bounds, -5F, -5F);
            using var pen = new Pen(AppTheme.Primary, 1.6F)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };
            DrawGlyph(e.Graphics, pen, icon);
        }

        private void DrawGlyph(Graphics graphics, Pen pen, RectangleF bounds)
        {
            float cx = bounds.Left + bounds.Width / 2F;
            float cy = bounds.Top + bounds.Height / 2F;
            switch (_icon)
            {
                case DashboardIcon.Calendar:
                    graphics.DrawRectangle(pen, bounds.X, bounds.Y + 1, bounds.Width, bounds.Height - 1);
                    graphics.DrawLine(pen, bounds.Left, bounds.Top + 4, bounds.Right, bounds.Top + 4);
                    break;
                case DashboardIcon.Activity:
                    graphics.DrawLines(pen, [
                        new PointF(bounds.Left, cy),
                        new PointF(cx - 3, cy),
                        new PointF(cx, bounds.Top),
                        new PointF(cx + 3, bounds.Bottom),
                        new PointF(bounds.Right, cy)]);
                    break;
                case DashboardIcon.Play:
                    graphics.DrawPolygon(pen, [
                        new PointF(bounds.Left + 2, bounds.Top),
                        new PointF(bounds.Right, cy),
                        new PointF(bounds.Left + 2, bounds.Bottom)]);
                    break;
                case DashboardIcon.Cloud:
                    graphics.DrawArc(pen, bounds.Left, cy - 2, bounds.Width * .55F, bounds.Height * .65F, 150, 210);
                    graphics.DrawArc(pen, cx - 2, bounds.Top, bounds.Width * .6F, bounds.Height * .8F, 190, 210);
                    graphics.DrawLine(pen, bounds.Left + 2, bounds.Bottom, bounds.Right - 1, bounds.Bottom);
                    break;
                case DashboardIcon.Music:
                    graphics.DrawLine(pen, cx, bounds.Top, cx, bounds.Bottom - 2);
                    graphics.DrawLine(pen, cx, bounds.Top, bounds.Right, bounds.Top + 2);
                    graphics.DrawEllipse(pen, bounds.Left, bounds.Bottom - 4, 5, 4);
                    break;
                case DashboardIcon.Check:
                    graphics.DrawEllipse(pen, bounds);
                    graphics.DrawLines(pen, [
                        new PointF(bounds.Left + 2, cy),
                        new PointF(cx - 1, bounds.Bottom - 2),
                        new PointF(bounds.Right - 1, bounds.Top + 2)]);
                    break;
                case DashboardIcon.ArrowUp:
                    graphics.DrawLine(pen, cx, bounds.Bottom, cx, bounds.Top);
                    graphics.DrawLine(pen, cx, bounds.Top, bounds.Left + 1, cy - 1);
                    graphics.DrawLine(pen, cx, bounds.Top, bounds.Right - 1, cy - 1);
                    break;
                case DashboardIcon.ArrowDown:
                    graphics.DrawLine(pen, cx, bounds.Top, cx, bounds.Bottom);
                    graphics.DrawLine(pen, cx, bounds.Bottom, bounds.Left + 1, cy + 1);
                    graphics.DrawLine(pen, cx, bounds.Bottom, bounds.Right - 1, cy + 1);
                    break;
            }
        }
    }
}
