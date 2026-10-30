using SubTrack.WinForms.Themes;

namespace SubTrack.WinForms.Controls;

internal class RoundedPanel : Card
{
    [System.ComponentModel.Browsable(false)]
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public int CornerRadius
    {
        get => Radius;
        set => Radius = value;
    }

    public static System.Drawing.Drawing2D.GraphicsPath CreateRoundedPath(Rectangle bounds, int radius) =>
        Shapes.Rounded(bounds, radius);
}
