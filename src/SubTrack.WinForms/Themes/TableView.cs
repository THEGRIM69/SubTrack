using System.Drawing.Drawing2D;

namespace SubTrack.WinForms.Themes;

internal static class TableGrid
{
    public static DataGridView Create(params (string Header, float Weight)[] columns)
    {
        var grid = new DataGridView
        {
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            AutoGenerateColumns = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = AppTheme.Surface,
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
            ColumnHeadersHeight = 36,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            Dock = DockStyle.Fill,
            EnableHeadersVisualStyles = false,
            GridColor = AppTheme.Border,
            MultiSelect = false,
            ReadOnly = true,
            RowHeadersVisible = false,
            RowTemplate = { Height = 42 },
            SelectionMode = DataGridViewSelectionMode.FullRowSelect
        };

        grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = AppTheme.Surface,
            Font = AppTheme.Font(9.5F),
            ForeColor = AppTheme.TextPrimary,
            SelectionBackColor = AppTheme.PrimarySoft,
            SelectionForeColor = AppTheme.TextPrimary,
            Padding = new Padding(8, 0, 8, 0)
        };
        grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(250, 252, 252),
            SelectionBackColor = AppTheme.PrimarySoft,
            SelectionForeColor = AppTheme.TextPrimary
        };
        grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = AppTheme.WindowBackground,
            Font = AppTheme.Font(8.25F, FontStyle.Bold),
            ForeColor = AppTheme.TextSecondary,
            Padding = new Padding(8, 0, 8, 0),
            SelectionBackColor = AppTheme.WindowBackground,
            SelectionForeColor = AppTheme.TextSecondary
        };

        for (int index = 0; index < columns.Length; index++)
        {
            (string header, float weight) = columns[index];
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = header,
                Name = $"Column{index}",
                FillWeight = Math.Max(weight, 1F),
                SortMode = DataGridViewColumnSortMode.NotSortable
            });
        }

        return grid;
    }
}

internal static class TableView
{
    public static void Show(
        IWin32Window? owner,
        string title,
        string subtitle,
        (string Header, float Weight)[] columns,
        IEnumerable<string[]> rows)
    {
        using var form = new TableViewerForm { Text = title };
        DataGridView grid = TableGrid.Create(columns);

        foreach (string[] row in rows)
        {
            grid.Rows.Add(row);
        }

        var titleLabel = new Label
        {
            Text = title,
            Dock = DockStyle.Fill,
            Font = AppTheme.Font(15F, FontStyle.Bold),
            ForeColor = AppTheme.TextPrimary,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = Padding.Empty
        };
        var subtitleLabel = new Label
        {
            Text = subtitle,
            Dock = DockStyle.Fill,
            Font = AppTheme.Font(9F),
            ForeColor = AppTheme.TextSecondary,
            TextAlign = ContentAlignment.TopLeft,
            Margin = Padding.Empty
        };

        var titles = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = Padding.Empty
        };
        titles.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        titles.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        titles.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        titles.Controls.Add(titleLabel, 0, 0);
        titles.Controls.Add(subtitleLabel, 0, 1);

        var close = new FlatButton("Cerrar", ButtonKind.Ghost)
        {
            Margin = new Padding(12, 4, 0, 0),
            DialogResult = DialogResult.Cancel
        };
        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        header.Controls.Add(titles, 0, 0);
        header.Controls.Add(close, 1, 0);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = Padding.Empty
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 66F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.Controls.Add(header, 0, 0);
        layout.Controls.Add(grid, 0, 1);
        form.Controls.Add(layout);

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
        BackColor = AppTheme.Surface;
        Font = AppTheme.Font(9.5F);
        Padding = new Padding(24, 18, 24, 22);
        DoubleBuffered = true;
        AutoScaleMode = AutoScaleMode.None;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var pen = new Pen(AppTheme.BorderStrong);
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
        using GraphicsPath path = Shapes.Rounded(new Rectangle(0, 0, Width, Height), 16);
        Region = new Region(path);
    }
}