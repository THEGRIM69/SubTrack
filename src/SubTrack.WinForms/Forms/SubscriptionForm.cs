using System.Globalization;
using SubTrack.WinForms.Models;
using SubTrack.WinForms.Themes;

namespace SubTrack.WinForms.Forms;

internal sealed class SubscriptionForm : Form
{
    private readonly bool _isEditMode;
    private readonly TextBox _txtNombre;
    private readonly ComboBox _cmbCategoria;
    private readonly TextBox _txtImporte;
    private readonly ComboBox _cmbPeriodicidad;
    private readonly DateTimePicker _dtpProximoCobro;
    private readonly ComboBox _cmbEstado;

    private readonly Label _lblErrorNombre;
    private readonly Label _lblErrorImporte;
    private readonly Label _lblErrorPeriodicidad;

    [System.ComponentModel.Browsable(false)]
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public SubscriptionViewModel Subscription { get; }

    public SubscriptionForm(SubscriptionViewModel? subscriptionToEdit = null)
    {
        _isEditMode = subscriptionToEdit is not null;
        Subscription = subscriptionToEdit?.Clone() ?? new SubscriptionViewModel();

        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = AppTheme.Surface;
        ClientSize = new Size(500, 620);
        MinimumSize = new Size(460, 580);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        Text = _isEditMode ? "Editar suscripción" : "Nueva suscripción";

        // Main layout
        var mainLayout = new TableLayoutPanel
        {
            BackColor = AppTheme.Surface,
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = new Padding(32, 24, 32, 24),
            RowCount = 3
        };
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70F)); // Header
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // Fields
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F)); // Actions

        // Header
        var headerPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty
        };
        var lblTitle = new Label
        {
            AutoSize = true,
            Font = AppTheme.PanelTitleFont,
            ForeColor = AppTheme.TextPrimary,
            Location = Point.Empty,
            Text = _isEditMode ? "Editar suscripción" : "Nueva suscripción"
        };
        var lblSubtitle = new Label
        {
            AutoSize = true,
            Font = AppTheme.SubtitleFont,
            ForeColor = AppTheme.TextSecondary,
            Location = new Point(0, 26),
            Text = _isEditMode
                ? "Modifica los datos temporales del servicio (modo demostración)"
                : "Registra los datos del servicio recurrente (modo demostración)"
        };
        var badge = new Label
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            AutoSize = false,
            BackColor = AppTheme.PrimarySoft,
            Font = AppTheme.CaptionBoldFont,
            ForeColor = AppTheme.Primary,
            Height = 22,
            Location = new Point(310, 2),
            Text = "MEMORIA",
            TextAlign = ContentAlignment.MiddleCenter,
            Width = 90
        };
        headerPanel.Controls.Add(lblTitle);
        headerPanel.Controls.Add(lblSubtitle);
        headerPanel.Controls.Add(badge);

        // Fields Panel
        var fieldsPanel = new FlowLayoutPanel
        {
            AutoScroll = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            Margin = Padding.Empty,
            Padding = new Padding(0, 8, 0, 8),
            WrapContents = false
        };

        int fieldWidth = 416;

        // 1. Nombre
        fieldsPanel.Controls.Add(CreateFieldLabel("Nombre del servicio *", fieldWidth));
        _txtNombre = new TextBox
        {
            Font = AppTheme.NavigationFont,
            Text = Subscription.Nombre,
            Width = fieldWidth
        };
        fieldsPanel.Controls.Add(_txtNombre);
        _lblErrorNombre = CreateErrorLabel(fieldWidth);
        fieldsPanel.Controls.Add(_lblErrorNombre);

        // 2. Categoría
        fieldsPanel.Controls.Add(CreateFieldLabel("Categoría (opcional)", fieldWidth));
        _cmbCategoria = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDown,
            Font = AppTheme.NavigationFont,
            Width = fieldWidth
        };
        _cmbCategoria.Items.AddRange(SubscriptionViewModel.CategoriasSugeridas);
        _cmbCategoria.Text = Subscription.Categoria;
        fieldsPanel.Controls.Add(_cmbCategoria);
        fieldsPanel.Controls.Add(CreateSpacer());

        // 3. Importe
        fieldsPanel.Controls.Add(CreateFieldLabel("Importe en dólares ($) *", fieldWidth));
        _txtImporte = new TextBox
        {
            Font = AppTheme.NavigationFont,
            PlaceholderText = "ej. 12.99",
            Text = _isEditMode ? Subscription.MontoDecimal.ToString("F2", CultureInfo.InvariantCulture) : string.Empty,
            Width = fieldWidth
        };
        fieldsPanel.Controls.Add(_txtImporte);
        _lblErrorImporte = CreateErrorLabel(fieldWidth);
        fieldsPanel.Controls.Add(_lblErrorImporte);

        // 4. Periodicidad
        fieldsPanel.Controls.Add(CreateFieldLabel("Periodicidad *", fieldWidth));
        _cmbPeriodicidad = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = AppTheme.NavigationFont,
            Width = fieldWidth
        };
        _cmbPeriodicidad.Items.AddRange(SubscriptionViewModel.PeriodicidadesDisponibles);
        _cmbPeriodicidad.SelectedItem = Subscription.Periodicidad;
        if (_cmbPeriodicidad.SelectedIndex < 0)
        {
            _cmbPeriodicidad.SelectedIndex = 2; // Mensual
        }
        fieldsPanel.Controls.Add(_cmbPeriodicidad);
        _lblErrorPeriodicidad = CreateErrorLabel(fieldWidth);
        fieldsPanel.Controls.Add(_lblErrorPeriodicidad);

        // 5. Próximo cobro
        fieldsPanel.Controls.Add(CreateFieldLabel("Fecha del próximo cobro *", fieldWidth));
        _dtpProximoCobro = new DateTimePicker
        {
            CustomFormat = "dd/MM/yyyy",
            Font = AppTheme.NavigationFont,
            Format = DateTimePickerFormat.Custom,
            Value = Subscription.ProximoCobro.ToDateTime(TimeOnly.MinValue),
            Width = fieldWidth
        };
        fieldsPanel.Controls.Add(_dtpProximoCobro);
        var lblHintDate = new Label
        {
            AutoSize = true,
            Font = AppTheme.CaptionFont,
            ForeColor = AppTheme.TextSecondary,
            Margin = new Padding(0, 2, 0, 8),
            Text = "Dato temporal de presentación (regla F0-T31 pendiente de Core)."
        };
        fieldsPanel.Controls.Add(lblHintDate);

        // 6. Estado
        fieldsPanel.Controls.Add(CreateFieldLabel("Estado visual *", fieldWidth));
        _cmbEstado = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = AppTheme.NavigationFont,
            Width = fieldWidth
        };
        _cmbEstado.Items.Add("Activa");
        _cmbEstado.Items.Add("Pausada");
        _cmbEstado.SelectedItem = Subscription.Estado == SubscriptionStatus.Paused ? "Pausada" : "Activa";
        fieldsPanel.Controls.Add(_cmbEstado);

        // Footer Actions
        var actionsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Margin = Padding.Empty,
            Padding = new Padding(0, 10, 0, 0)
        };

        var btnGuardar = new Button
        {
            BackColor = AppTheme.Primary,
            Cursor = Cursors.Hand,
            FlatStyle = FlatStyle.Flat,
            Font = AppTheme.NavigationActiveFont,
            ForeColor = AppTheme.Surface,
            Height = 36,
            Margin = Padding.Empty,
            TabStop = true,
            Text = _isEditMode ? "Guardar cambios" : "Registrar suscripción",
            UseVisualStyleBackColor = false,
            Width = 160
        };
        btnGuardar.FlatAppearance.BorderSize = 0;
        btnGuardar.Click += OnGuardarClicked;

        var btnCancelar = new Button
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
            Text = "Cancelar",
            UseVisualStyleBackColor = false,
            Width = 100
        };
        btnCancelar.FlatAppearance.BorderColor = AppTheme.Border;
        btnCancelar.FlatAppearance.BorderSize = 1;
        btnCancelar.FlatAppearance.MouseOverBackColor = AppTheme.NavigationHover;

        actionsPanel.Controls.Add(btnGuardar);
        actionsPanel.Controls.Add(btnCancelar);

        mainLayout.Controls.Add(headerPanel, 0, 0);
        mainLayout.Controls.Add(fieldsPanel, 0, 1);
        mainLayout.Controls.Add(actionsPanel, 0, 2);

        Controls.Add(mainLayout);
        AcceptButton = btnGuardar;
        CancelButton = btnCancelar;
    }

    private static Label CreateFieldLabel(string text, int width) => new()
    {
        AutoSize = true,
        Font = AppTheme.CaptionBoldFont,
        ForeColor = AppTheme.TextPrimary,
        Margin = new Padding(0, 4, 0, 3),
        Text = text,
        Width = width
    };

    private static Label CreateErrorLabel(int width) => new()
    {
        AutoSize = true,
        Font = AppTheme.CaptionFont,
        ForeColor = Color.FromArgb(217, 45, 32),
        Margin = new Padding(0, 1, 0, 6),
        Text = string.Empty,
        Visible = false,
        Width = width
    };

    private static Control CreateSpacer() => new Panel
    {
        Height = 4,
        Margin = Padding.Empty,
        Width = 1
    };

    private void OnGuardarClicked(object? sender, EventArgs e)
    {
        bool isValid = ValidateForm();
        if (!isValid)
        {
            return;
        }

        // Apply validated changes to Subscription
        Subscription.Nombre = _txtNombre.Text.Trim();
        Subscription.Categoria = string.IsNullOrWhiteSpace(_cmbCategoria.Text) ? "General" : _cmbCategoria.Text.Trim();

        TryParseImporteToCentavos(_txtImporte.Text, out long centavos, out _);
        Subscription.MontoCentavos = centavos;

        Subscription.Periodicidad = _cmbPeriodicidad.SelectedItem?.ToString() ?? "Mensual";
        Subscription.ProximoCobro = DateOnly.FromDateTime(_dtpProximoCobro.Value);

        if (_cmbEstado.SelectedItem?.ToString() == "Pausada")
        {
            Subscription.Estado = SubscriptionStatus.Paused;
        }
        else
        {
            DateOnly today = DateOnly.FromDateTime(DateTime.Today);
            if (Subscription.ProximoCobro <= today.AddDays(7))
            {
                Subscription.Estado = SubscriptionStatus.DueSoon;
            }
            else
            {
                Subscription.Estado = SubscriptionStatus.Active;
            }
        }

        DialogResult = DialogResult.OK;
        Close();
    }

    private bool ValidateForm()
    {
        bool valid = true;

        // Validar Nombre
        if (string.IsNullOrWhiteSpace(_txtNombre.Text))
        {
            _lblErrorNombre.Text = "El nombre del servicio es obligatorio.";
            _lblErrorNombre.Visible = true;
            valid = false;
        }
        else
        {
            _lblErrorNombre.Visible = false;
        }

        // Validar Importe
        if (!TryParseImporteToCentavos(_txtImporte.Text, out _, out string errorMessage))
        {
            _lblErrorImporte.Text = errorMessage;
            _lblErrorImporte.Visible = true;
            valid = false;
        }
        else
        {
            _lblErrorImporte.Visible = false;
        }

        // Validar Periodicidad
        if (_cmbPeriodicidad.SelectedItem is null)
        {
            _lblErrorPeriodicidad.Text = "Debes seleccionar una periodicidad.";
            _lblErrorPeriodicidad.Visible = true;
            valid = false;
        }
        else
        {
            _lblErrorPeriodicidad.Visible = false;
        }

        return valid;
    }

    private static bool TryParseImporteToCentavos(string input, out long centavos, out string errorMessage)
    {
        centavos = 0;
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(input))
        {
            errorMessage = "El importe es obligatorio.";
            return false;
        }

        string normalized = input.Trim().Replace("$", "").Trim();
        normalized = normalized.Replace(',', '.');

        if (!decimal.TryParse(normalized, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal val))
        {
            errorMessage = "El importe debe ser un número válido.";
            return false;
        }

        if (val <= 0m)
        {
            errorMessage = "El importe debe ser mayor que cero.";
            return false;
        }

        decimal rounded = Math.Round(val, 2, MidpointRounding.AwayFromZero);
        if (val != rounded)
        {
            errorMessage = "El importe no puede tener más de dos decimales.";
            return false;
        }

        centavos = (long)(rounded * 100m);
        if (centavos <= 0)
        {
            errorMessage = "El importe debe ser mayor que cero.";
            return false;
        }

        return true;
    }
}
