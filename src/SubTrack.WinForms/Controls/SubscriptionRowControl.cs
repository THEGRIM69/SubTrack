using SubTrack.WinForms.Models;
using SubTrack.WinForms.Themes;

namespace SubTrack.WinForms.Controls;

internal sealed class SubscriptionRowControl : UserControl
{
    private readonly SubscriptionViewModel _subscription;
    private readonly TableLayoutPanel _layout;
    private readonly Label _serviceLabel;
    private readonly Label _categoryLabel;
    private readonly Label _amountLabel;
    private readonly Label _recurrenceLabel;
    private readonly Label _nextChargeLabel;
    private readonly Label _statusBadge;
    private readonly Button _detailButton;
    private readonly Button _editButton;
    private bool _isHovered;

    public event EventHandler<SubscriptionViewModel>? DetailRequested;
    public event EventHandler<SubscriptionViewModel>? EditRequested;

    [System.ComponentModel.Browsable(false)]
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public SubscriptionViewModel Subscription => _subscription;

    public SubscriptionRowControl(SubscriptionViewModel subscription)
    {
        _subscription = subscription;

        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = AppTheme.Surface;
        Dock = DockStyle.Top;
        Height = 54;
        Margin = Padding.Empty;
        Padding = new Padding(18, 0, 18, 0);
        DoubleBuffered = true;
        TabStop = true;

        AccessibleRole = AccessibleRole.ListItem;
        AccessibleName = $"{subscription.Nombre}, {subscription.MontoFormateado}, {subscription.Periodicidad}, {subscription.EstadoTexto}";

        _layout = new TableLayoutPanel
        {
            BackColor = AppTheme.Surface,
            ColumnCount = 6,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            RowCount = 1
        };
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 108F));
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112F));
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 124F));
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112F));
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 144F));
        _layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        // Column 0: Service & Category
        var serviceCell = new TableLayoutPanel
        {
            BackColor = AppTheme.Surface,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = new Padding(0, 7, 8, 7),
            RowCount = 2
        };
        serviceCell.RowStyles.Add(new RowStyle(SizeType.Percent, 52F));
        serviceCell.RowStyles.Add(new RowStyle(SizeType.Percent, 48F));

        _serviceLabel = new Label
        {
            AutoEllipsis = true,
            BackColor = AppTheme.Surface,
            Dock = DockStyle.Fill,
            Font = AppTheme.ItemBoldFont,
            ForeColor = AppTheme.TextPrimary,
            Margin = Padding.Empty,
            Text = subscription.Nombre,
            TextAlign = ContentAlignment.BottomLeft
        };
        _categoryLabel = new Label
        {
            AutoEllipsis = true,
            BackColor = AppTheme.Surface,
            Dock = DockStyle.Fill,
            Font = AppTheme.CaptionFont,
            ForeColor = AppTheme.TextSecondary,
            Margin = Padding.Empty,
            Text = string.IsNullOrWhiteSpace(subscription.Categoria) ? "General" : subscription.Categoria,
            TextAlign = ContentAlignment.TopLeft
        };
        serviceCell.Controls.Add(_serviceLabel, 0, 0);
        serviceCell.Controls.Add(_categoryLabel, 0, 1);

        // Column 1: Amount
        _amountLabel = new Label
        {
            AutoEllipsis = false,
            BackColor = AppTheme.Surface,
            Dock = DockStyle.Fill,
            Font = AppTheme.ItemBoldFont,
            ForeColor = AppTheme.TextPrimary,
            Margin = Padding.Empty,
            Text = subscription.MontoFormateado,
            TextAlign = ContentAlignment.MiddleRight
        };

        // Column 2: Recurrence
        _recurrenceLabel = new Label
        {
            AutoEllipsis = true,
            BackColor = AppTheme.Surface,
            Dock = DockStyle.Fill,
            Font = AppTheme.NavigationFont,
            ForeColor = AppTheme.TextSecondary,
            Margin = Padding.Empty,
            Text = subscription.Periodicidad,
            TextAlign = ContentAlignment.MiddleRight
        };

        // Column 3: Next Charge
        _nextChargeLabel = new Label
        {
            AutoEllipsis = true,
            BackColor = AppTheme.Surface,
            Dock = DockStyle.Fill,
            Font = AppTheme.NavigationFont,
            ForeColor = AppTheme.TextSecondary,
            Margin = Padding.Empty,
            Text = subscription.ProximoCobroFormateado,
            TextAlign = ContentAlignment.MiddleRight
        };

        // Column 4: Status Pill
        var statusHost = new TableLayoutPanel
        {
            BackColor = AppTheme.Surface,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            RowCount = 3
        };
        statusHost.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        statusHost.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        statusHost.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

        Color statusForeground = subscription.Estado switch
        {
            SubscriptionStatus.Active => AppTheme.Success,
            SubscriptionStatus.DueSoon => AppTheme.Warning,
            _ => AppTheme.TextSecondary
        };
        Color statusBackground = subscription.Estado switch
        {
            SubscriptionStatus.Active => Color.FromArgb(231, 247, 239),
            SubscriptionStatus.DueSoon => Color.FromArgb(255, 244, 225),
            _ => Color.FromArgb(237, 239, 243)
        };

        _statusBadge = new Label
        {
            Anchor = AnchorStyles.Right,
            AutoSize = true,
            BackColor = statusBackground,
            Font = AppTheme.CaptionBoldFont,
            ForeColor = statusForeground,
            Margin = Padding.Empty,
            Padding = new Padding(8, 4, 8, 4),
            Text = subscription.EstadoTexto,
            TextAlign = ContentAlignment.MiddleCenter
        };
        statusHost.Controls.Add(_statusBadge, 0, 1);

        // Column 5: Actions
        var actionsHost = new FlowLayoutPanel
        {
            Anchor = AnchorStyles.Right,
            AutoSize = true,
            BackColor = AppTheme.Surface,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = Padding.Empty,
            Padding = new Padding(0, 12, 0, 0),
            WrapContents = false
        };

        _detailButton = new Button
        {
            AccessibleName = $"Ver detalles de {subscription.Nombre}",
            BackColor = AppTheme.PrimarySoft,
            Cursor = Cursors.Hand,
            FlatStyle = FlatStyle.Flat,
            Font = AppTheme.CaptionBoldFont,
            ForeColor = AppTheme.Primary,
            Height = 28,
            Margin = new Padding(0, 0, 6, 0),
            TabStop = true,
            Text = "Detalle",
            UseVisualStyleBackColor = false,
            Width = 62
        };
        _detailButton.FlatAppearance.BorderSize = 0;
        _detailButton.Click += (_, _) => DetailRequested?.Invoke(this, _subscription);

        _editButton = new Button
        {
            AccessibleName = $"Editar suscripción {subscription.Nombre}",
            BackColor = AppTheme.Surface,
            Cursor = Cursors.Hand,
            FlatStyle = FlatStyle.Flat,
            Font = AppTheme.CaptionBoldFont,
            ForeColor = AppTheme.TextPrimary,
            Height = 28,
            Margin = Padding.Empty,
            TabStop = true,
            Text = "Editar",
            UseVisualStyleBackColor = false,
            Width = 56
        };
        _editButton.FlatAppearance.BorderColor = AppTheme.Border;
        _editButton.FlatAppearance.BorderSize = 1;
        _editButton.FlatAppearance.MouseOverBackColor = AppTheme.NavigationHover;
        _editButton.Click += (_, _) => EditRequested?.Invoke(this, _subscription);

        actionsHost.Controls.Add(_detailButton);
        actionsHost.Controls.Add(_editButton);

        _layout.Controls.Add(serviceCell, 0, 0);
        _layout.Controls.Add(_amountLabel, 1, 0);
        _layout.Controls.Add(_recurrenceLabel, 2, 0);
        _layout.Controls.Add(_nextChargeLabel, 3, 0);
        _layout.Controls.Add(statusHost, 4, 0);
        _layout.Controls.Add(actionsHost, 5, 0);

        Controls.Add(_layout);

        AttachHoverEvents(this);
        AttachHoverEvents(_layout);
        AttachHoverEvents(serviceCell);
        AttachHoverEvents(_serviceLabel);
        AttachHoverEvents(_categoryLabel);
        AttachHoverEvents(_amountLabel);
        AttachHoverEvents(_recurrenceLabel);
        AttachHoverEvents(_nextChargeLabel);
        AttachHoverEvents(statusHost);
        AttachHoverEvents(actionsHost);
    }

    private void AttachHoverEvents(Control control)
    {
        control.MouseEnter += (_, _) => SetHovered(true);
        control.MouseLeave += (_, _) =>
        {
            Point mousePos = PointToClient(Cursor.Position);
            if (!ClientRectangle.Contains(mousePos))
            {
                SetHovered(false);
            }
        };
    }

    private void SetHovered(bool hovered)
    {
        if (_isHovered == hovered)
        {
            return;
        }

        _isHovered = hovered;
        Color bg = hovered ? AppTheme.NavigationHover : AppTheme.Surface;
        BackColor = bg;
        _layout.BackColor = bg;
        _serviceLabel.BackColor = bg;
        _categoryLabel.BackColor = bg;
        _amountLabel.BackColor = bg;
        _recurrenceLabel.BackColor = bg;
        _nextChargeLabel.BackColor = bg;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var pen = new Pen(AppTheme.Border, 1);
        e.Graphics.DrawLine(pen, 18, Height - 1, Width - 18, Height - 1);
    }
}
