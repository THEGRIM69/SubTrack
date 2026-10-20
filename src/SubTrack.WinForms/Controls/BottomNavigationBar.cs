using SubTrack.WinForms.Themes;

namespace SubTrack.WinForms.Controls;

internal sealed class BottomNavigationBar : UserControl
{
    private readonly FlowLayoutPanel _itemsPanel;
    private readonly Dictionary<string, Button> _buttons = [];

    public event EventHandler<string>? SectionSelected;

    public BottomNavigationBar()
    {
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        BackColor = AppTheme.Surface;
        Padding = new Padding(8, 7, 8, 7);
        Margin = Padding.Empty;

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

        AddItem("Dashboard", "▦");
        AddItem("Suscripciones", "▤");
        AddItem("Cobros", "◷");
        AddItem("Pagos", "✓");
        AddItem("Gastos", "−");
        AddItem("Ingresos", "+");
    }

    public void SetActiveSection(string section)
    {
        foreach ((string name, Button button) in _buttons)
        {
            bool isActive = name == section;
            button.BackColor = isActive ? AppTheme.PrimarySoft : AppTheme.Surface;
            button.ForeColor = isActive ? AppTheme.Primary : AppTheme.TextSecondary;
            button.Font = new Font(AppTheme.NavigationFont, isActive ? FontStyle.Bold : FontStyle.Regular);
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var pen = new Pen(AppTheme.Border);
        e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
    }

    private void AddItem(string name, string icon)
    {
        var button = new Button
        {
            AccessibleName = name,
            BackColor = AppTheme.Surface,
            Cursor = Cursors.Hand,
            FlatStyle = FlatStyle.Flat,
            Font = AppTheme.NavigationFont,
            ForeColor = AppTheme.TextSecondary,
            Height = 54,
            Margin = new Padding(2, 0, 2, 0),
            TabStop = true,
            Text = $"{icon}\r\n{name}",
            TextAlign = ContentAlignment.MiddleCenter,
            UseVisualStyleBackColor = false,
            Width = name == "Suscripciones" ? 116 : 96
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = AppTheme.PrimarySoft;
        button.Click += (_, _) => SectionSelected?.Invoke(this, name);

        _buttons.Add(name, button);
        _itemsPanel.Controls.Add(button);
    }
}
