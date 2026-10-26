namespace SubTrack.WinForms.Theme;

/// <summary>Visor modal de solo lectura para historiales y versiones.</summary>
internal static class TableView
{
    public static void Show(
        IWin32Window? owner,
        string title,
        string subtitle,
        (string Header, float Weight)[] columns,
        IEnumerable<string[]> rows)
    {
        using var form = new TableViewerForm
        {
            Text = title
        };

        var titles = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0)
        };

        titles.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        titles.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var titleLabel = new Label
        {
            Text = title,
            Dock = DockStyle.Fill,
            Font = Palette.Font(15f, FontStyle.Bold),
            ForeColor = Palette.Text,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var subtitleLabel = new Label
        {
            Text = subtitle,
            Dock = DockStyle.Fill,
            Font = Palette.Font(9f),
            ForeColor = Palette.Muted,
            TextAlign = ContentAlignment.TopLeft
        };

        titles.Controls.Add(titleLabel, 0, 0);
        titles.Controls.Add(subtitleLabel, 0, 1);

        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 66,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0)
        };

        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        header.Controls.Add(titles, 0, 0);

        var close = new FlatButton("Cerrar", ButtonKind.Ghost)
        {
            Margin = new Padding(12, 4, 0, 0),
            DialogResult = DialogResult.Cancel
        };

        header.Controls.Add(close, 1, 0);

        var grid = Ui.Grid(columns);

        foreach (var row in rows)
        {
            grid.Rows.Add(row);
        }

        form.Controls.Add(grid);
        form.Controls.Add(header);

        form.CancelButton = close;
        Native.Drag(header);
        Native.Drag(titles);
        Native.Drag(titleLabel);
        Native.Drag(subtitleLabel);

        form.ShowDialog(owner);
    }
}

internal sealed class TableViewerForm : Form
{
    public TableViewerForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        MaximizeBox = false;
        MinimizeBox = false;
        KeyPreview = true;
        Size = new Size(840, 520);
        MinimumSize = new Size(640, 400);
        BackColor = Palette.Surface;
        Font = Palette.Font(9.5f);
        Padding = new Padding(24, 18, 24, 22);
        DoubleBuffered = true;
        AutoScaleMode = AutoScaleMode.None;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        using var pen = new Pen(Palette.BorderStrong);
        e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);

        if (Width <= 0 || Height <= 0)
        {
            return;
        }

        Region?.Dispose();
        Region = new Region(Shapes.Rounded(new Rectangle(0, 0, Width, Height), 16));
    }
}
