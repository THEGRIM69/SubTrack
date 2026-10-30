using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace SubTrack.WinForms.Themes;

internal sealed class FormDialog : Form
{
    private readonly TableLayoutPanel _fields;
    private readonly FlatButton _accept;
    private int _row;

    public FormDialog(string title, string description = "", string acceptText = "Guardar")
    {
        Text = title;
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        MaximizeBox = false;
        MinimizeBox = false;
        KeyPreview = true;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        MinimumSize = new Size(416, 0);
        BackColor = AppTheme.Surface;
        Font = AppTheme.Font(9.5F);
        Padding = new Padding(28, 22, 28, 24);
        DoubleBuffered = true;
        AutoScaleMode = AutoScaleMode.None;

        var header = new TableLayoutPanel
        {
            ColumnCount = 2,
            RowCount = 1,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Top,
            Margin = Padding.Empty
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var titleLabel = new Label
        {
            Text = title,
            AutoSize = true,
            Font = AppTheme.Font(15F, FontStyle.Bold),
            ForeColor = AppTheme.TextPrimary,
            Margin = new Padding(0, 0, 8, 0)
        };

        var close = new FlatButton("X", ButtonKind.Ghost)
        {
            Width = 30,
            Height = 30,
            MinimumSize = new Size(30, 30),
            Margin = Padding.Empty,
            AccessibleName = "Cerrar"
        };
        close.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        header.Controls.Add(titleLabel, 0, 0);
        header.Controls.Add(close, 1, 0);

        var descriptionLabel = new Label
        {
            Text = description,
            AutoSize = true,
            MaximumSize = new Size(350, 0),
            Font = AppTheme.Font(9.25F),
            ForeColor = AppTheme.TextSecondary,
            Margin = new Padding(0, 6, 0, 12),
            Visible = !string.IsNullOrWhiteSpace(description)
        };

        _fields = new TableLayoutPanel
        {
            ColumnCount = 1,
            RowCount = 0,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Top,
            Margin = Padding.Empty
        };
        _fields.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _accept = new FlatButton(acceptText, ButtonKind.Primary)
        {
            Margin = new Padding(8, 0, 0, 0),
            DialogResult = DialogResult.OK
        };
        var cancel = new FlatButton("Cancelar", ButtonKind.Ghost)
        {
            Margin = Padding.Empty,
            DialogResult = DialogResult.Cancel
        };

        var buttons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Top,
            Margin = new Padding(0, 14, 0, 0),
            Padding = Padding.Empty
        };
        buttons.Controls.Add(_accept);
        buttons.Controls.Add(cancel);

        var root = new TableLayoutPanel
        {
            ColumnCount = 1,
            RowCount = 4,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty
        };
        for (int index = 0; index < 4; index++)
        {
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        root.Controls.Add(header, 0, 0);
        root.Controls.Add(descriptionLabel, 0, 1);
        root.Controls.Add(_fields, 0, 2);
        root.Controls.Add(buttons, 0, 3);
        Controls.Add(root);

        CancelButton = cancel;
        AcceptButton = _accept;
        Native.Drag(header);
        Native.Drag(titleLabel);
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ButtonKind AcceptKind
    {
        set
        {
            _accept.Kind = value;
            _accept.Invalidate();
        }
    }

    public TextBox AddText(string label, string value = "", int maxLength = 120)
    {
        var box = new TextBox
        {
            Text = value,
            MaxLength = maxLength,
            Font = AppTheme.Font(10F),
            BorderStyle = BorderStyle.None,
            Margin = Padding.Empty
        };
        Add(label, Host(box));
        return box;
    }

    public NumericUpDown AddMoney(string label, decimal value = 0M)
    {
        var box = new NumericUpDown
        {
            DecimalPlaces = 2,
            Maximum = 1_000_000_000M,
            ThousandsSeparator = true,
            Font = AppTheme.Font(10F),
            BorderStyle = BorderStyle.None,
            Value = Math.Clamp(value, 0M, 1_000_000_000M),
            Margin = Padding.Empty
        };
        Add(label, Host(box));
        return box;
    }

    public NumericUpDown AddNumber(string label, decimal value, decimal minimum, decimal maximum)
    {
        var box = new NumericUpDown
        {
            DecimalPlaces = 0,
            Minimum = minimum,
            Maximum = maximum,
            Font = AppTheme.Font(10F),
            BorderStyle = BorderStyle.None,
            Value = Math.Clamp(value, minimum, maximum),
            Margin = Padding.Empty
        };
        Add(label, Host(box));
        return box;
    }

    public DateTimePicker AddDate(string label, DateTime? value = null, bool maxToday = false)
    {
        var picker = new DateTimePicker
        {
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "dd/MM/yyyy",
            Font = AppTheme.Font(9.5F),
            Value = value ?? DateTime.Today,
            Margin = Padding.Empty
        };
        if (maxToday)
        {
            picker.MaxDate = DateTime.Today;
        }

        Add(label, Host(picker));
        return picker;
    }

    public ComboBox AddCombo<T>(string label, IReadOnlyList<(T Value, string Text)> options, T selected)
    {
        ComboBox combo = Combo.Create(options, selected, 316);
        combo.Margin = Padding.Empty;
        Add(label, Host(combo));
        return combo;
    }

    public CheckBox AddCheck(string label, bool value = false)
    {
        var check = new CheckBox
        {
            Text = label,
            Checked = value,
            AutoSize = true,
            Font = AppTheme.Font(9.5F),
            ForeColor = AppTheme.TextPrimary,
            Margin = new Padding(0, 0, 0, 14)
        };
        Add(string.Empty, check, host: false);
        return check;
    }

    public void AddNote(string text)
    {
        var note = new Label
        {
            Text = text,
            AutoSize = true,
            MaximumSize = new Size(340, 0),
            ForeColor = AppTheme.TextSecondary,
            Font = AppTheme.Font(8.25F, FontStyle.Italic),
            Margin = new Padding(0, 0, 0, 12)
        };
        _fields.RowCount = _row + 1;
        _fields.Controls.Add(note, 0, _row++);
    }

    public static T Value<T>(ComboBox combo) => Combo.Value<T>(combo);

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var pen = new Pen(AppTheme.BorderStrong);
        e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        UpdateRegion();
    }

    private void Add(string label, Control input, bool host = true)
    {
        int row = _row;
        _fields.RowCount = row + (label.Length == 0 ? 1 : 2);

        if (label.Length > 0)
        {
            _fields.Controls.Add(new Label
            {
                Text = label,
                AutoSize = true,
                Font = AppTheme.Font(8.25F, FontStyle.Bold),
                ForeColor = AppTheme.TextSecondary,
                Margin = new Padding(0, 0, 0, 5)
            }, 0, row++);
        }

        if (host)
        {
            input.Margin = new Padding(0, 0, 0, 14);
        }

        _fields.Controls.Add(input, 0, row);
        _row = row + 1;
    }

    private static Control Host(Control input)
    {
        var host = new Panel
        {
            Height = 40,
            Width = 336,
            BackColor = AppTheme.Surface,
            Padding = new Padding(10, 8, 10, 8),
            Margin = Padding.Empty
        };
        host.Paint += (_, args) =>
        {
            args.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using GraphicsPath path = Shapes.Rounded(new Rectangle(0, 0, host.Width - 1, host.Height - 1), 9);
            using var pen = new Pen(AppTheme.BorderStrong);
            args.Graphics.DrawPath(pen, path);
        };

        if (input is TextBox)
        {
            input.Dock = DockStyle.Fill;
        }
        else
        {
            input.Left = 10;
            input.Width = 316;
            input.Top = Math.Max(2, (host.Height - input.Height) / 2);
            input.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        }

        host.Controls.Add(input);
        return host;
    }

    private void UpdateRegion()
    {
        if (Width <= 0 || Height <= 0)
        {
            return;
        }

        Region?.Dispose();
        Region = new Region(Shapes.Rounded(new Rectangle(0, 0, Width, Height), 14));
    }
}

internal static class Confirm
{
    public static bool Ask(
        IWin32Window? owner,
        string title,
        string message,
        string acceptText = "Confirmar",
        bool danger = false)
    {
        using var dialog = new FormDialog(title, message, acceptText)
        {
            AcceptKind = danger ? ButtonKind.Danger : ButtonKind.Primary
        };

        return dialog.ShowDialog(owner) == DialogResult.OK;
    }
}