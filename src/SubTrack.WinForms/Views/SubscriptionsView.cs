using System.Drawing.Drawing2D;
using SubTrack.WinForms.Themes;

namespace SubTrack.WinForms.Views;

internal sealed class SubscriptionsView : UserControl
{
    private readonly Panel _heading;
    private readonly MetricCard[] _metrics;
    private readonly RoundedPanel _subscriptionsList;

    public SubscriptionsView()
    {
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScroll = true;
        BackColor = AppTheme.WindowBackground;
        Dock = DockStyle.Fill;

        _heading = CreateHeading();
        _metrics =
        [
            new MetricCard("Suscripciones activas", "4", AppTheme.Primary),
            new MetricCard("Próximas a vencer", "2", AppTheme.Warning),
            new MetricCard("Gasto mensual estimado", "$52.46", AppTheme.Success)
        ];
        _subscriptionsList = CreateSubscriptionsList();

        Controls.Add(_heading);
        foreach (MetricCard metric in _metrics)
        {
            Controls.Add(metric);
        }
        Controls.Add(_subscriptionsList);

        PerformLayout();
    }

    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);
        if (_heading is null || _metrics is null || _metrics.Length != 3 || _subscriptionsList is null)
        {
            return;
        }

        int horizontalMargin = ScaleLogical(40);
        int top = ScaleLogical(30);
        int headingHeight = ScaleLogical(58);
        int sectionGap = ScaleLogical(18);
        int cardGap = ScaleLogical(14);
        int metricHeight = ScaleLogical(96);
        int listHeight = ScaleLogical(340);
        int bottom = ScaleLogical(28);
        int scrollAllowance = VerticalScroll.Visible ? SystemInformation.VerticalScrollBarWidth : 0;
        int availableWidth = Math.Max(
            ScaleLogical(560),
            ClientSize.Width - (horizontalMargin * 2) - scrollAllowance);

        _heading.SetBounds(horizontalMargin, top, availableWidth, headingHeight);
        int metricsTop = top + headingHeight + sectionGap;
        int metricsBottom;

        if (availableWidth >= ScaleLogical(900))
        {
            int metricWidth = (availableWidth - (cardGap * 2)) / 3;
            for (int index = 0; index < _metrics.Length; index++)
            {
                _metrics[index].SetBounds(
                    horizontalMargin + (index * (metricWidth + cardGap)),
                    metricsTop,
                    metricWidth,
                    metricHeight);
            }
            metricsBottom = metricsTop + metricHeight;
        }
        else if (availableWidth >= ScaleLogical(620))
        {
            int metricWidth = (availableWidth - cardGap) / 2;
            _metrics[0].SetBounds(horizontalMargin, metricsTop, metricWidth, metricHeight);
            _metrics[1].SetBounds(horizontalMargin + metricWidth + cardGap, metricsTop, metricWidth, metricHeight);
            _metrics[2].SetBounds(
                horizontalMargin,
                metricsTop + metricHeight + cardGap,
                availableWidth,
                metricHeight);
            metricsBottom = metricsTop + (metricHeight * 2) + cardGap;
        }
        else
        {
            for (int index = 0; index < _metrics.Length; index++)
            {
                _metrics[index].SetBounds(
                    horizontalMargin,
                    metricsTop + (index * (metricHeight + cardGap)),
                    availableWidth,
                    metricHeight);
            }
            metricsBottom = metricsTop + (metricHeight * 3) + (cardGap * 2);
        }

        int listTop = metricsBottom + sectionGap;
        _subscriptionsList.SetBounds(horizontalMargin, listTop, availableWidth, listHeight);
        SetAutoScrollMinSize(new Size(0, listTop + listHeight + bottom));
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
            Text = "Suscripciones"
        };
        var subtitle = new Label
        {
            AutoSize = true,
            Font = AppTheme.SubtitleFont,
            ForeColor = AppTheme.TextSecondary,
            Location = new Point(2, 38),
            Text = "Administra tus servicios y pagos recurrentes"
        };
        var demoBadge = new Label
        {
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
        var newSubscription = new Button
        {
            AccessibleDescription = "Funcionalidad pendiente de implementación",
            AccessibleName = "Nueva suscripción",
            BackColor = AppTheme.Primary,
            Enabled = false,
            FlatStyle = FlatStyle.Flat,
            Font = AppTheme.NavigationActiveFont,
            ForeColor = AppTheme.Surface,
            Height = 34,
            Text = "Nueva suscripción",
            UseVisualStyleBackColor = false,
            Width = 148
        };
        newSubscription.FlatAppearance.BorderSize = 0;
        newSubscription.Top = 1;

        heading.Resize += (_, _) =>
        {
            newSubscription.Left = heading.ClientSize.Width - newSubscription.Width;
            demoBadge.Left = newSubscription.Left - demoBadge.Width - 12;
        };
        heading.Controls.Add(title);
        heading.Controls.Add(subtitle);
        heading.Controls.Add(demoBadge);
        heading.Controls.Add(newSubscription);
        return heading;
    }

    private static RoundedPanel CreateSubscriptionsList()
    {
        var card = new RoundedPanel
        {
            BackColor = AppTheme.Surface,
            Padding = new Padding(1)
        };
        var layout = new TableLayoutPanel
        {
            BackColor = AppTheme.Surface,
            ColumnCount = 5,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = new Padding(18, 10, 18, 12),
            RowCount = 7
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 108F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 124F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        for (int row = 0; row < 5; row++)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
        }

        var sectionTitle = new Label
        {
            Dock = DockStyle.Fill,
            Font = AppTheme.PanelTitleFont,
            ForeColor = AppTheme.TextPrimary,
            Margin = Padding.Empty,
            Text = "Listado de suscripciones",
            TextAlign = ContentAlignment.MiddleLeft
        };
        layout.Controls.Add(sectionTitle, 0, 0);
        layout.SetColumnSpan(sectionTitle, 5);

        string[] columns = ["SERVICIO", "IMPORTE", "PERIODICIDAD", "PRÓXIMO COBRO", "ESTADO"];
        for (int column = 0; column < columns.Length; column++)
        {
            layout.Controls.Add(CreateColumnHeader(columns[column], column == 0), column, 1);
        }

        AddSubscription(layout, 2, "StreamWave", "Entretenimiento · Ejemplo", "$12.99", "Mensual", "28 oct 2026", "Activa", SubscriptionStatus.Active);
        AddSubscription(layout, 3, "CloudBox Pro", "Almacenamiento · Ejemplo", "$8.50", "Mensual", "02 nov 2026", "Próxima", SubscriptionStatus.DueSoon);
        AddSubscription(layout, 4, "MusicFlow", "Música · Ejemplo", "$6.99", "Mensual", "06 nov 2026", "Activa", SubscriptionStatus.Active);
        AddSubscription(layout, 5, "DesignKit", "Diseño · Ejemplo", "$15.00", "Mensual", "11 nov 2026", "Pausada", SubscriptionStatus.Paused);
        AddSubscription(layout, 6, "LearnSpace", "Educación · Ejemplo", "$8.98", "Mensual", "18 nov 2026", "Activa", SubscriptionStatus.Active);

        card.Controls.Add(layout);
        return card;
    }

    private static Label CreateColumnHeader(string text, bool alignLeft) => new()
    {
        Dock = DockStyle.Fill,
        Font = AppTheme.CaptionBoldFont,
        ForeColor = AppTheme.TextSecondary,
        Margin = Padding.Empty,
        Text = text,
        TextAlign = alignLeft ? ContentAlignment.MiddleLeft : ContentAlignment.MiddleRight
    };

    private static void AddSubscription(
        TableLayoutPanel layout,
        int row,
        string service,
        string description,
        string amount,
        string recurrence,
        string nextCharge,
        string status,
        SubscriptionStatus statusKind)
    {
        layout.Controls.Add(CreateServiceCell(service, description), 0, row);
        layout.Controls.Add(CreateValueCell(amount, AppTheme.TextPrimary, AppTheme.ItemBoldFont), 1, row);
        layout.Controls.Add(CreateValueCell(recurrence, AppTheme.TextSecondary, AppTheme.NavigationFont), 2, row);
        layout.Controls.Add(CreateValueCell(nextCharge, AppTheme.TextSecondary, AppTheme.NavigationFont), 3, row);
        layout.Controls.Add(CreateStatusCell(status, statusKind), 4, row);
    }

    private static Control CreateServiceCell(string service, string description)
    {
        var cell = new TableLayoutPanel
        {
            BackColor = AppTheme.Surface,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 1, 0, 0),
            Padding = new Padding(0, 4, 8, 4),
            RowCount = 2
        };
        cell.RowStyles.Add(new RowStyle(SizeType.Percent, 52F));
        cell.RowStyles.Add(new RowStyle(SizeType.Percent, 48F));
        cell.Controls.Add(new Label
        {
            AutoEllipsis = true,
            Dock = DockStyle.Fill,
            Font = AppTheme.ItemBoldFont,
            ForeColor = AppTheme.TextPrimary,
            Margin = Padding.Empty,
            Text = service,
            TextAlign = ContentAlignment.BottomLeft
        }, 0, 0);
        cell.Controls.Add(new Label
        {
            AutoEllipsis = true,
            Dock = DockStyle.Fill,
            Font = AppTheme.CaptionFont,
            ForeColor = AppTheme.TextSecondary,
            Margin = Padding.Empty,
            Text = description,
            TextAlign = ContentAlignment.TopLeft
        }, 0, 1);
        return cell;
    }

    private static Label CreateValueCell(string text, Color color, Font font) => new()
    {
        AutoEllipsis = false,
        BackColor = AppTheme.Surface,
        Dock = DockStyle.Fill,
        Font = font,
        ForeColor = color,
        Margin = new Padding(0, 1, 0, 0),
        Text = text,
        TextAlign = ContentAlignment.MiddleRight
    };

    private static Control CreateStatusCell(string text, SubscriptionStatus status)
    {
        Color foreground = status switch
        {
            SubscriptionStatus.Active => AppTheme.Success,
            SubscriptionStatus.DueSoon => AppTheme.Warning,
            _ => AppTheme.TextSecondary
        };
        Color background = status switch
        {
            SubscriptionStatus.Active => Color.FromArgb(231, 247, 239),
            SubscriptionStatus.DueSoon => Color.FromArgb(255, 244, 225),
            _ => Color.FromArgb(237, 239, 243)
        };
        var host = new TableLayoutPanel
        {
            BackColor = AppTheme.Surface,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 1, 0, 0),
            RowCount = 3
        };
        host.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        host.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        host.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        host.Controls.Add(new Label
        {
            Anchor = AnchorStyles.Right,
            AutoSize = true,
            BackColor = background,
            Font = AppTheme.CaptionBoldFont,
            ForeColor = foreground,
            Margin = Padding.Empty,
            Padding = new Padding(8, 4, 8, 4),
            Text = text,
            TextAlign = ContentAlignment.MiddleCenter
        }, 0, 1);
        return host;
    }

    private enum SubscriptionStatus
    {
        Active,
        DueSoon,
        Paused
    }

    private sealed class MetricCard : RoundedPanel
    {
        public MetricCard(string caption, string value, Color accent)
        {
            BackColor = AppTheme.Surface;
            Padding = new Padding(18, 12, 18, 12);

            var accentLine = new Panel
            {
                BackColor = accent,
                Dock = DockStyle.Left,
                Margin = Padding.Empty,
                Width = 4
            };
            var valueLabel = new Label
            {
                Dock = DockStyle.Fill,
                Font = AppTheme.PanelTitleFont,
                ForeColor = accent,
                Margin = Padding.Empty,
                Padding = new Padding(14, 23, 0, 0),
                Text = value,
                TextAlign = ContentAlignment.MiddleLeft
            };
            var captionLabel = new Label
            {
                Dock = DockStyle.Top,
                Font = AppTheme.NavigationFont,
                ForeColor = AppTheme.TextSecondary,
                Height = 28,
                Margin = Padding.Empty,
                Padding = new Padding(14, 0, 0, 0),
                Text = caption,
                TextAlign = ContentAlignment.MiddleLeft
            };
            Controls.Add(valueLabel);
            Controls.Add(captionLabel);
            Controls.Add(accentLine);
        }
    }

    private class RoundedPanel : Panel
    {
        private const int CornerRadius = 14;

        public RoundedPanel()
        {
            DoubleBuffered = true;
            Margin = Padding.Empty;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle bounds = ClientRectangle;
            bounds.Width--;
            bounds.Height--;
            using GraphicsPath path = CreateRoundedPath(bounds);
            using var pen = new Pen(AppTheme.Border);
            e.Graphics.DrawPath(pen, path);
        }

        protected override void OnResize(EventArgs eventargs)
        {
            base.OnResize(eventargs);
            Rectangle bounds = ClientRectangle;
            bounds.Width--;
            bounds.Height--;
            using GraphicsPath path = CreateRoundedPath(bounds);
            Region? oldRegion = Region;
            Region = new Region(path);
            oldRegion?.Dispose();
        }

        private static GraphicsPath CreateRoundedPath(Rectangle bounds)
        {
            var path = new GraphicsPath();
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                return path;
            }

            int diameter = CornerRadius * 2;
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
}
