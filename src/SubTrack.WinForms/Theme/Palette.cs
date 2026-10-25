using System.Drawing;
using System.Drawing.Text;

namespace SubTrack.WinForms.Theme;

/// <summary>
/// Tokens visuales: paleta Petronas (verde/negro) con superficies claras al estilo Apple.
/// </summary>
internal static class Palette
{
    public static readonly Color Ink = Color.FromArgb(10, 14, 17);
    public static readonly Color InkSoft = Color.FromArgb(20, 26, 31);
    public static readonly Color InkHover = Color.FromArgb(31, 39, 45);

    public static readonly Color Canvas = Color.FromArgb(245, 247, 248);
    public static readonly Color Surface = Color.FromArgb(255, 255, 255);

    public static readonly Color Border = Color.FromArgb(228, 233, 236);
    public static readonly Color BorderStrong = Color.FromArgb(209, 218, 223);

    public static readonly Color Text = Color.FromArgb(15, 23, 32);
    public static readonly Color Muted = Color.FromArgb(108, 120, 134);

    public static readonly Color Primary = Color.FromArgb(0, 210, 190);
    public static readonly Color PrimaryDeep = Color.FromArgb(0, 161, 156);
    public static readonly Color PrimarySoft = Color.FromArgb(226, 249, 246);

    public static readonly Color Danger = Color.FromArgb(203, 66, 66);
    public static readonly Color DangerSoft = Color.FromArgb(252, 236, 236);

    public static readonly Color Warning = Color.FromArgb(176, 127, 8);
    public static readonly Color WarningSoft = Color.FromArgb(253, 245, 224);

    public static readonly string Family = ResolveFamily();

    private static readonly Dictionary<(float Size, FontStyle Style), Font> Fonts = new();

    public static Font Font(float size, FontStyle style = FontStyle.Regular)
    {
        if (!Fonts.TryGetValue((size, style), out var font))
        {
            font = new Font(Family, size, style, GraphicsUnit.Point);
            Fonts[(size, style)] = font;
        }

        return font;
    }

    private static string ResolveFamily()
    {
        using var installed = new InstalledFontCollection();
        var names = installed.Families.Select(family => family.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (names.Contains("Segoe UI Variable Text"))
        {
            return "Segoe UI Variable Text";
        }

        return names.Contains("Segoe UI") ? "Segoe UI" : SystemFonts.MessageBoxFont!.FontFamily.Name;
    }
}
