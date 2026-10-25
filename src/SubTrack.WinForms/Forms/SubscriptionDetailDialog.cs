using System.Globalization;
using SubTrack.WinForms.Models;
using SubTrack.WinForms.Themes;

namespace SubTrack.WinForms.Forms;

internal sealed class SubscriptionDetailDialog : Form
{
    private readonly SubscriptionViewModel _subscription;
    [System.ComponentModel.Browsable(false)]
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public bool WasEdited { get; private set; }

    public SubscriptionDetailDialog(SubscriptionViewModel subscription)
    {
        _subscription = subscription;

        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = AppTheme.Surface;
        ClientSize = new Size(460, 500);
        MinimumSize = new Size(420, 460);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        Text = $"Detalle de {_subscription.Nombre}";

        var layout = new TableLayoutPanel
        {
            BackColor = AppTheme.Surface,
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = new Padding(28, 24, 28, 24),
            RowCount = 3
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70F));  // Header
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // Details Card
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));  // Actions

        // 1. Header
        var header = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty
        };
        var lblTitle = new Label
        {
            AutoEllipsis = true,
            Font = AppTheme.PanelTitleFont,
            ForeColor = AppTheme.TextPrimary,
            Location = Point.Empty,
            Size = new Size(270, 26),
            Text = _subscription.Nombre
        };
        var lblCategory = new Label
        {
            AutoEllipsis = true,
            Font = AppTheme.SubtitleFont,
            ForeColor = AppTheme.TextSecondary,
            Location = new Point(0, 26),
            Size = new Size(270, 22),
            Text = string.IsNullOrWhiteSpace(_subscription.Categoria) ? "General" : _subscription.Categoria
        };

        Color statusFg = _subscription.Estado switch
        {
            SubscriptionStatus.Active => AppTheme.Success,
            SubscriptionStatus.DueSoon => AppTheme.Warning,
            _ => AppTheme.TextSecondary
        };
        Color statusBg = _subscription.Estado switch
        {
            SubscriptionStatus.Active => Color.FromArgb(231, 247, 239),
            SubscriptionStatus.DueSoon => Color.FromArgb(255, 244, 225),
            _ => Color.FromArgb(237, 239, 243)
        };

        var statusBadge = new Label
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            AutoSize = true,
            BackColor = statusBg,
            Font = AppTheme.CaptionBoldFont,
            ForeColor = statusFg,
            Location = new Point(310, 4),
            Padding = new Padding(8, 4, 8, 4),
            Text = _subscription.EstadoTexto
        };

        header.Controls.Add(lblTitle);
        header.Controls.Add(lblCategory);
        header.Controls.Add(statusBadge);

        // 2. Details Card
        var detailsCard = new TableLayoutPanel
        {
            BackColor = AppTheme.WindowBackground,
            ColumnCount = 2,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 8, 0, 8),
            Padding = new Padding(16, 12, 16, 12),
            RowCount = 5
        };
        detailsCard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));
        detailsCard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));
        for (int i = 0; i < 5; i++)
        {
            detailsCard.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
        }

        AddDetailRow(detailsCard, 0, "Importe registrado:", $"{_subscription.MontoFormateado} ({_subscription.MontoCentavos} centavos)");
        AddDetailRow(detailsCard, 1, "Periodicidad:", _subscription.Periodicidad);

        string fechaLarga = _subscription.ProximoCobro.ToString("D", CultureInfo.GetCultureInfo("es-ES"));
        AddDetailRow(detailsCard, 2, "Próximo cobro:", fechaLarga);
        AddDetailRow(detailsCard, 3, "ID temporal:", _subscription.Id.ToString()[..8] + "...");
        AddDetailRow(detailsCard, 4, "Modo de datos:", "Memoria temporal (UI-01)");

        // 3. Actions
        var actionsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Margin = Padding.Empty,
            Padding = new Padding(0, 10, 0, 0)
        };

        var btnEditar = new Button
        {
            BackColor = AppTheme.Primary,
            Cursor = Cursors.Hand,
            FlatStyle = FlatStyle.Flat,
            Font = AppTheme.NavigationActiveFont,
            ForeColor = AppTheme.Surface,
            Height = 36,
            Margin = Padding.Empty,
            TabStop = true,
            Text = "Editar suscripción",
            UseVisualStyleBackColor = false,
            Width = 140
        };
        btnEditar.FlatAppearance.BorderSize = 0;
        btnEditar.Click += (_, _) =>
        {
            using var editForm = new SubscriptionForm(_subscription);
            if (editForm.ShowDialog(this) == DialogResult.OK)
            {
                // Copy edited values
                _subscription.Nombre = editForm.Subscription.Nombre;
                _subscription.Categoria = editForm.Subscription.Categoria;
                _subscription.MontoCentavos = editForm.Subscription.MontoCentavos;
                _subscription.Periodicidad = editForm.Subscription.Periodicidad;
                _subscription.ProximoCobro = editForm.Subscription.ProximoCobro;
                _subscription.Estado = editForm.Subscription.Estado;

                WasEdited = true;
                DialogResult = DialogResult.OK;
                Close();
            }
        };

        var btnCerrar = new Button
        {
            BackColor = AppTheme.Surface,
            Cursor = Cursors.Hand,
            DialogResult = DialogResult.Cancel,
            FlatStyle = FlatStyle.Flat,
            Font = AppTheme.NavigationFont,
            ForeColor = AppTheme.TextPrimary,
            Height = 36,
            Margin = new Padding(0, 0, 10, 0),
            TabStop = true,
            Text = "Cerrar",
            UseVisualStyleBackColor = false,
            Width = 90
        };
        btnCerrar.FlatAppearance.BorderColor = AppTheme.Border;
        btnCerrar.FlatAppearance.BorderSize = 1;
        btnCerrar.FlatAppearance.MouseOverBackColor = AppTheme.NavigationHover;

        actionsPanel.Controls.Add(btnEditar);
        actionsPanel.Controls.Add(btnCerrar);

        layout.Controls.Add(header, 0, 0);
        layout.Controls.Add(detailsCard, 0, 1);
        layout.Controls.Add(actionsPanel, 0, 2);

        Controls.Add(layout);
        CancelButton = btnCerrar;
    }

    private static void AddDetailRow(TableLayoutPanel table, int row, string label, string value)
    {
        var lbl = new Label
        {
            AutoEllipsis = true,
            Dock = DockStyle.Fill,
            Font = AppTheme.CaptionBoldFont,
            ForeColor = AppTheme.TextSecondary,
            Margin = Padding.Empty,
            Text = label,
            TextAlign = ContentAlignment.MiddleLeft
        };
        var val = new Label
        {
            AutoEllipsis = true,
            Dock = DockStyle.Fill,
            Font = AppTheme.NavigationFont,
            ForeColor = AppTheme.TextPrimary,
            Margin = Padding.Empty,
            Text = value,
            TextAlign = ContentAlignment.MiddleLeft
        };
        table.Controls.Add(lbl, 0, row);
        table.Controls.Add(val, 1, row);
    }
}
