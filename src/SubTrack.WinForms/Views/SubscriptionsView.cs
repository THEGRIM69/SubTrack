using System.Drawing.Drawing2D;
using System.Globalization;
using SubTrack.WinForms.Controls;
using SubTrack.WinForms.Forms;
using SubTrack.WinForms.Models;
using SubTrack.WinForms.Themes;

namespace SubTrack.WinForms.Views;

internal sealed class SubscriptionsView : UserControl
{
    private readonly List<SubscriptionViewModel> _subscriptions = [];
    private readonly Panel _heading;
    private readonly MetricCard[] _metrics;
    private readonly RoundedPanel _subscriptionsListCard;
    private readonly TableLayoutPanel _listTable;
    private readonly Label _countBadge;
    private readonly EmptyStateView _emptyState;
    private readonly Button _newSubscriptionButton;

    public SubscriptionsView()
    {
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScroll = true;
        BackColor = AppTheme.WindowBackground;
        Dock = DockStyle.Fill;

        _heading = CreateHeading(out _newSubscriptionButton);
        _newSubscriptionButton.Click += (_, _) => OpenNewSubscriptionDialog();

        _metrics =
        [
            new MetricCard("Suscripciones activas", "0", AppTheme.Primary),
            new MetricCard("Próximas a vencer", "0", AppTheme.Warning),
            new MetricCard("Gasto mensual estimado", "$0.00", AppTheme.Success)
        ];

        _subscriptionsListCard = new RoundedPanel
        {
            BackColor = AppTheme.Surface,
            Padding = new Padding(1)
        };

        _listTable = new TableLayoutPanel
        {
            BackColor = AppTheme.Surface,
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        _listTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _subscriptionsListCard.Controls.Add(_listTable);

        _countBadge = new Label
        {
            Anchor = AnchorStyles.Right,
            AutoSize = true,
            BackColor = AppTheme.PrimarySoft,
            Font = AppTheme.CaptionBoldFont,
            ForeColor = AppTheme.Primary,
            Margin = Padding.Empty,
            Padding = new Padding(8, 4, 8, 4),
            Text = "0 registros"
        };

        _emptyState = new EmptyStateView(
            "Suscripciones",
            "Aún no tienes suscripciones registradas en memoria.",
            "📋",
            "+ Nueva suscripción",
            (_, _) => OpenNewSubscriptionDialog(),
            "No hay suscripciones registradas",
            "Registra tus suscripciones para controlar cobros, periodicidades y gastos estimados en modo demostración.");
        _emptyState.Visible = false;

        Controls.Add(_heading);
        foreach (MetricCard metric in _metrics)
        {
            Controls.Add(metric);
        }
        Controls.Add(_subscriptionsListCard);
        Controls.Add(_emptyState);

        SeedInitialDemoData();
        RefreshView();
    }

    private void SeedInitialDemoData()
    {
        _subscriptions.Clear();
        _subscriptions.AddRange(
        [
            new SubscriptionViewModel
            {
                Nombre = "StreamWave",
                Categoria = "Entretenimiento · Ejemplo",
                MontoCentavos = 1299,
                Periodicidad = "Mensual",
                ProximoCobro = new DateOnly(2026, 10, 28),
                Estado = SubscriptionStatus.Active
            },
            new SubscriptionViewModel
            {
                Nombre = "CloudBox Pro",
                Categoria = "Almacenamiento · Ejemplo",
                MontoCentavos = 850,
                Periodicidad = "Mensual",
                ProximoCobro = new DateOnly(2026, 11, 2),
                Estado = SubscriptionStatus.DueSoon
            },
            new SubscriptionViewModel
            {
                Nombre = "MusicFlow",
                Categoria = "Música · Ejemplo",
                MontoCentavos = 699,
                Periodicidad = "Mensual",
                ProximoCobro = new DateOnly(2026, 11, 6),
                Estado = SubscriptionStatus.Active
            },
            new SubscriptionViewModel
            {
                Nombre = "DesignKit",
                Categoria = "Diseño · Ejemplo",
                MontoCentavos = 1500,
                Periodicidad = "Mensual",
                ProximoCobro = new DateOnly(2026, 11, 11),
                Estado = SubscriptionStatus.Paused
            },
            new SubscriptionViewModel
            {
                Nombre = "LearnSpace",
                Categoria = "Educación · Ejemplo",
                MontoCentavos = 898,
                Periodicidad = "Mensual",
                ProximoCobro = new DateOnly(2026, 11, 18),
                Estado = SubscriptionStatus.Active
            }
        ]);
    }

    private void RefreshView()
    {
        // 1. Recalcular métricas
        int activas = _subscriptions.Count(s => s.Estado != SubscriptionStatus.Paused);
        int proximas = _subscriptions.Count(s => s.Estado == SubscriptionStatus.DueSoon);

        var mensuales = _subscriptions.Where(s => s.Estado != SubscriptionStatus.Paused && s.Periodicidad == "Mensual").ToList();
        long totalMensualCentavos = mensuales.Sum(s => s.MontoCentavos);
        decimal totalMensualDecimal = totalMensualCentavos / 100m;

        bool tieneNoMensuales = _subscriptions.Any(s => s.Estado != SubscriptionStatus.Paused && s.Periodicidad != "Mensual");

        _metrics[0].SetValue(activas.ToString(CultureInfo.InvariantCulture));
        _metrics[1].SetValue(proximas.ToString(CultureInfo.InvariantCulture));

        if (tieneNoMensuales)
        {
            _metrics[2].SetValue($"${totalMensualDecimal:F2}*", "* Parcial: solo incluye periodicidad mensual");
        }
        else
        {
            _metrics[2].SetValue($"${totalMensualDecimal:F2}");
        }

        // 2. Gestionar estado vacío o listado
        if (_subscriptions.Count == 0)
        {
            _subscriptionsListCard.Visible = false;
            _emptyState.Visible = true;
        }
        else
        {
            _emptyState.Visible = false;
            _subscriptionsListCard.Visible = true;
            RebuildListTable();
        }

        PerformLayout();
        Invalidate(true);
    }

    private void RebuildListTable()
    {
        _listTable.SuspendLayout();
        _listTable.Controls.Clear();
        _listTable.RowStyles.Clear();

        _countBadge.Text = _subscriptions.Count == 1 ? "1 suscripción" : $"{_subscriptions.Count} suscripciones";

        // Row 0: Section Title Panel
        _listTable.RowStyles.Add(new RowStyle(SizeType.Absolute, ScaleLogical(48)));
        var titlePanel = new TableLayoutPanel
        {
            BackColor = AppTheme.Surface,
            ColumnCount = 2,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = new Padding(18, 0, 18, 0)
        };
        titlePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        titlePanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        titlePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var sectionTitle = new Label
        {
            Dock = DockStyle.Fill,
            Font = AppTheme.PanelTitleFont,
            ForeColor = AppTheme.TextPrimary,
            Margin = Padding.Empty,
            Text = "Listado de suscripciones",
            TextAlign = ContentAlignment.MiddleLeft
        };
        titlePanel.Controls.Add(sectionTitle, 0, 0);
        titlePanel.Controls.Add(_countBadge, 1, 0);
        _listTable.Controls.Add(titlePanel, 0, 0);

        // Row 1: Columns Header
        _listTable.RowStyles.Add(new RowStyle(SizeType.Absolute, ScaleLogical(34)));
        var tableHeader = new TableLayoutPanel
        {
            BackColor = AppTheme.Surface,
            ColumnCount = 6,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = new Padding(18, 0, 18, 0)
        };
        tableHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tableHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 108F));
        tableHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112F));
        tableHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 124F));
        tableHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112F));
        tableHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 144F));
        tableHeader.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        tableHeader.Controls.Add(CreateColumnHeader("SERVICIO", true), 0, 0);
        tableHeader.Controls.Add(CreateColumnHeader("IMPORTE", false), 1, 0);
        tableHeader.Controls.Add(CreateColumnHeader("PERIODICIDAD", false), 2, 0);
        tableHeader.Controls.Add(CreateColumnHeader("PRÓXIMO COBRO", false), 3, 0);
        tableHeader.Controls.Add(CreateColumnHeader("ESTADO", false), 4, 0);
        tableHeader.Controls.Add(CreateColumnHeader("ACCIONES", false), 5, 0);
        _listTable.Controls.Add(tableHeader, 0, 1);

        // Rows 2..N+1: Subscription items
        int rowHeight = ScaleLogical(54);
        for (int i = 0; i < _subscriptions.Count; i++)
        {
            SubscriptionViewModel sub = _subscriptions[i];
            _listTable.RowStyles.Add(new RowStyle(SizeType.Absolute, rowHeight));

            var rowControl = new SubscriptionRowControl(sub);
            rowControl.DetailRequested += (_, item) => OpenDetailDialog(item);
            rowControl.EditRequested += (_, item) => OpenEditSubscriptionDialog(item);

            _listTable.Controls.Add(rowControl, 0, i + 2);
        }

        _listTable.RowCount = 2 + _subscriptions.Count;
        _listTable.ResumeLayout(true);
    }

    private void OpenNewSubscriptionDialog()
    {
        using var form = new SubscriptionForm();
        if (form.ShowDialog(FindForm()) == DialogResult.OK)
        {
            _subscriptions.Insert(0, form.Subscription);
            RefreshView();
        }
    }

    private void OpenEditSubscriptionDialog(SubscriptionViewModel subscription)
    {
        using var form = new SubscriptionForm(subscription);
        if (form.ShowDialog(FindForm()) == DialogResult.OK)
        {
            int index = _subscriptions.FindIndex(s => s.Id == subscription.Id);
            if (index >= 0)
            {
                _subscriptions[index] = form.Subscription;
            }
            RefreshView();
        }
    }

    private void OpenDetailDialog(SubscriptionViewModel subscription)
    {
        using var dialog = new SubscriptionDetailDialog(subscription);
        dialog.ShowDialog(FindForm());
        if (dialog.WasEdited)
        {
            RefreshView();
        }
    }

    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);
        if (_heading is null || _metrics is null || _metrics.Length != 3 || _subscriptionsListCard is null)
        {
            return;
        }

        int horizontalMargin = ScaleLogical(40);
        int top = ScaleLogical(30);
        int headingHeight = ScaleLogical(58);
        int sectionGap = ScaleLogical(18);
        int cardGap = ScaleLogical(14);
        int metricHeight = ScaleLogical(98);
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

        int contentTop = metricsBottom + sectionGap;

        if (_subscriptions.Count == 0)
        {
            _subscriptionsListCard.Visible = false;
            _emptyState.Visible = true;
            int emptyHeight = ScaleLogical(360);
            _emptyState.SetBounds(horizontalMargin, contentTop, availableWidth, emptyHeight);
            SetAutoScrollMinSize(new Size(0, contentTop + emptyHeight + bottom));
        }
        else
        {
            _emptyState.Visible = false;
            _subscriptionsListCard.Visible = true;

            int rowHeight = ScaleLogical(54);
            int headerAreaHeight = ScaleLogical(84);
            int rowsTotalHeight = _subscriptions.Count * rowHeight;
            int listCardHeight = headerAreaHeight + rowsTotalHeight + ScaleLogical(8);

            _subscriptionsListCard.SetBounds(horizontalMargin, contentTop, availableWidth, listCardHeight);
            SetAutoScrollMinSize(new Size(0, contentTop + listCardHeight + bottom));
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

    private static Panel CreateHeading(out Button newSubscription)
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
            Text = "Administra tus servicios y pagos recurrentes (modo demostración)"
        };
        var demoBadge = new Label
        {
            AutoSize = false,
            BackColor = AppTheme.PrimarySoft,
            Font = AppTheme.CaptionBoldFont,
            ForeColor = AppTheme.Primary,
            Height = 25,
            Text = "DATOS EN MEMORIA",
            TextAlign = ContentAlignment.MiddleCenter,
            Top = 5,
            Width = 160
        };
        newSubscription = new Button
        {
            AccessibleDescription = "Registrar una nueva suscripción en memoria",
            AccessibleName = "Nueva suscripción",
            BackColor = AppTheme.Primary,
            Cursor = Cursors.Hand,
            Enabled = true,
            FlatStyle = FlatStyle.Flat,
            Font = AppTheme.NavigationActiveFont,
            ForeColor = AppTheme.Surface,
            Height = 34,
            Text = "+ Nueva suscripción",
            UseVisualStyleBackColor = false,
            Width = 160
        };
        newSubscription.FlatAppearance.BorderSize = 0;
        newSubscription.Top = 1;

        Button btnRef = newSubscription;
        heading.Resize += (_, _) =>
        {
            btnRef.Left = heading.ClientSize.Width - btnRef.Width;
            demoBadge.Left = btnRef.Left - demoBadge.Width - 12;
        };
        heading.Controls.Add(title);
        heading.Controls.Add(subtitle);
        heading.Controls.Add(demoBadge);
        heading.Controls.Add(newSubscription);
        return heading;
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

    private sealed class MetricCard : RoundedPanel
    {
        private readonly Label _valueLabel;
        private readonly Label _noteLabel;

        public MetricCard(string caption, string value, Color accent)
        {
            BackColor = AppTheme.Surface;
            Padding = new Padding(18, 12, 18, 8);

            var accentLine = new Panel
            {
                BackColor = accent,
                Dock = DockStyle.Left,
                Margin = Padding.Empty,
                Width = 4
            };
            var captionLabel = new Label
            {
                Dock = DockStyle.Top,
                Font = AppTheme.NavigationFont,
                ForeColor = AppTheme.TextSecondary,
                Height = 22,
                Margin = Padding.Empty,
                Padding = new Padding(14, 0, 0, 0),
                Text = caption,
                TextAlign = ContentAlignment.MiddleLeft
            };
            _valueLabel = new Label
            {
                Dock = DockStyle.Fill,
                Font = AppTheme.PanelTitleFont,
                ForeColor = accent,
                Margin = Padding.Empty,
                Padding = new Padding(14, 2, 0, 0),
                Text = value,
                TextAlign = ContentAlignment.MiddleLeft
            };
            _noteLabel = new Label
            {
                Dock = DockStyle.Bottom,
                Font = AppTheme.CaptionFont,
                ForeColor = AppTheme.TextSecondary,
                Height = 18,
                Margin = Padding.Empty,
                Padding = new Padding(14, 0, 0, 0),
                Text = string.Empty,
                TextAlign = ContentAlignment.MiddleLeft,
                Visible = false
            };

            Controls.Add(_valueLabel);
            Controls.Add(_noteLabel);
            Controls.Add(captionLabel);
            Controls.Add(accentLine);
        }

        public void SetValue(string value, string? note = null)
        {
            _valueLabel.Text = value;
            if (!string.IsNullOrEmpty(note))
            {
                _noteLabel.Text = note;
                _noteLabel.Visible = true;
            }
            else
            {
                _noteLabel.Text = string.Empty;
                _noteLabel.Visible = false;
            }
        }
    }
}
