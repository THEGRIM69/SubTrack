using System.Drawing.Drawing2D;
using SubTrack.WinForms.Theme;

namespace SubTrack.WinForms.Views;

internal sealed class AboutForm : Form
{
    public AboutForm()
    {
        Text = "Acerca de SubTrack 1.0";
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        MaximizeBox = false;
        MinimizeBox = false;
        Size = new Size(620, 580);
        MinimumSize = Size;
        MaximumSize = Size;
        BackColor = Palette.Canvas;
        Font = Palette.Font(9.5f);
        Padding = new Padding(1);
        DoubleBuffered = true;
        AutoScaleMode = AutoScaleMode.None;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
            BackColor = Palette.Canvas,
            Margin = Padding.Empty,
            Padding = new Padding(23, 18, 23, 20)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 104));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 190));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 145));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

        var heading = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Palette.Canvas,
            Margin = Padding.Empty
        };
        heading.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        heading.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 34));

        var headingText = new Label
        {
            Text = "Acerca de SubTrack 1.0",
            Dock = DockStyle.Fill,
            Font = Palette.Font(15f, FontStyle.Bold),
            ForeColor = Palette.Text,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = Padding.Empty
        };
        var closeIcon = new FlatButton("✕", ButtonKind.Ghost)
        {
            AccessibleName = "Cerrar ventana Acerca de",
            Width = 32,
            Height = 32,
            MinimumSize = new Size(32, 32),
            Margin = Padding.Empty
        };
        closeIcon.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        heading.Controls.Add(headingText, 0, 0);
        heading.Controls.Add(closeIcon, 1, 0);
        root.Controls.Add(heading, 0, 0);
        root.Controls.Add(BuildIdentity(), 0, 1);
        root.Controls.Add(BuildDetailsCard("Tecnologías utilizadas",
        [
            ("Lenguaje", "C#"),
            ("Framework", ".NET 10"),
            ("Interfaz", "Windows Forms (WinForms)"),
            ("Base de datos", "SQLite"),
            ("Pruebas", "xUnit"),
            ("Entorno de desarrollo", "Visual Studio")
        ]), 0, 2);
        root.Controls.Add(BuildDevelopers(), 0, 3);

        var close = new FlatButton("Cerrar", ButtonKind.Primary)
        {
            DialogResult = DialogResult.Cancel,
            Margin = new Padding(0),
            Anchor = AnchorStyles.Right
        };
        root.Controls.Add(close, 0, 5);

        Controls.Add(root);
        CancelButton = close;
        Native.Drag(heading);
        Native.Drag(headingText);
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
        Region = new Region(Shapes.Rounded(new Rectangle(0, 0, Width, Height), 14));
    }

    private static Control BuildIdentity()
    {
        var card = new Card
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18, 12, 18, 12),
            Margin = new Padding(0, 0, 0, 12)
        };
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Palette.Surface,
            Margin = Padding.Empty
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(new Label
        {
            Text = "SubTrack 1.0",
            Dock = DockStyle.Fill,
            Font = Palette.Font(18f, FontStyle.Bold),
            ForeColor = Palette.PrimaryDeep,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = Padding.Empty
        }, 0, 0);
        layout.Controls.Add(new Label
        {
            Text = "Aplicación de escritorio para la gestión y seguimiento de suscripciones, cobros, pagos, gastos e ingresos.",
            Dock = DockStyle.Fill,
            Font = Palette.Font(9.5f),
            ForeColor = Palette.Muted,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = Padding.Empty
        }, 0, 1);
        card.Controls.Add(layout);

        return card;
    }

    private static Control BuildDetailsCard(string title, (string Label, string Value)[] details)
    {
        var card = new Card
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18, 10, 18, 10),
            Margin = new Padding(0, 0, 0, 12)
        };
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = details.Length + 1,
            BackColor = Palette.Surface,
            Margin = Padding.Empty
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        layout.Controls.Add(Ui.CardTitle(title), 0, 0);
        layout.SetColumnSpan(layout.GetControlFromPosition(0, 0)!, 2);

        for (var index = 0; index < details.Length; index++)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / details.Length));
            layout.Controls.Add(CreateDetailLabel(details[index].Label, true), 0, index + 1);
            layout.Controls.Add(CreateDetailLabel(details[index].Value, false), 1, index + 1);
        }

        card.Controls.Add(layout);
        return card;
    }

    private static Control BuildDevelopers()
    {
        var card = new Card
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18, 10, 18, 10),
            Margin = new Padding(0)
        };
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Palette.Surface,
            Margin = Padding.Empty
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        layout.Controls.Add(Ui.CardTitle("Desarrolladores"), 0, 0);

        string[] developers =
        [
            "Ing. Josue Adolfo Perez Hernandez",
            "Ing. Rodrigo Rivera Flores",
            "Ing. Henrry Giovany Marroquin Valle",
            "Ing. Pamela Michelle Tejada Cruz"
        ];
        for (var index = 0; index < developers.Length; index++)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / developers.Length));
            layout.Controls.Add(CreateDetailLabel(developers[index], false), 0, index + 1);
        }

        card.Controls.Add(layout);
        return card;
    }

    private static Label CreateDetailLabel(string text, bool isLabel) => new()
    {
        Text = text,
        Dock = DockStyle.Fill,
        Font = Palette.Font(9.5f, isLabel ? FontStyle.Bold : FontStyle.Regular),
        ForeColor = isLabel ? Palette.Muted : Palette.Text,
        TextAlign = ContentAlignment.MiddleLeft,
        AutoEllipsis = true,
        Margin = new Padding(0)
    };
}
