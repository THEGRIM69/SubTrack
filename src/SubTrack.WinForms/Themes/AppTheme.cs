using System.Drawing.Text;

namespace SubTrack.WinForms.Themes;

internal static class AppTheme
{
    public static readonly Color Ink = Color.FromArgb(10, 14, 17);
    public static readonly Color InkSoft = Color.FromArgb(20, 26, 31);
    public static readonly Color InkHover = Color.FromArgb(31, 39, 45);
    public static readonly Color WindowBackground = Color.FromArgb(245, 247, 248);
    public static readonly Color Surface = Color.White;
    public static readonly Color Border = Color.FromArgb(228, 233, 236);
    public static readonly Color BorderStrong = Color.FromArgb(209, 218, 223);
    public static readonly Color TextPrimary = Color.FromArgb(15, 23, 32);
    public static readonly Color TextSecondary = Color.FromArgb(108, 120, 134);
    public static readonly Color Primary = Color.FromArgb(0, 210, 190);
    public static readonly Color PrimaryDeep = Color.FromArgb(0, 161, 156);
    public static readonly Color PrimarySoft = Color.FromArgb(226, 249, 246);
    public static readonly Color PrimaryPressed = Color.FromArgb(204, 239, 235);
    public static readonly Color NavigationHover = Color.FromArgb(244, 247, 248);
    public static readonly Color Success = PrimaryDeep;
    public static readonly Color Warning = Color.FromArgb(176, 127, 8);
    public static readonly Color WarningSoft = Color.FromArgb(253, 245, 224);
    public static readonly Color Danger = Color.FromArgb(203, 66, 66);
    public static readonly Color DangerSoft = Color.FromArgb(252, 236, 236);

    public static readonly string FontFamily = ResolveFontFamily();

    private static readonly Dictionary<(float Size, FontStyle Style), Font> Fonts = [];

    public static readonly Font BrandFont = Font(16F, FontStyle.Bold);
    public static readonly Font TitleFont = Font(22F, FontStyle.Bold);
    public static readonly Font SubtitleFont = Font(10.5F);
    public static readonly Font SearchFont = Font(10F);
    public static readonly Font NavigationFont = Font(9F);
    public static readonly Font NavigationActiveFont = Font(9F, FontStyle.Bold);
    public static readonly Font PanelTitleFont = Font(12F, FontStyle.Bold);
    public static readonly Font ItemBoldFont = Font(9.5F, FontStyle.Bold);
    public static readonly Font CaptionFont = Font(8.5F);
    public static readonly Font CaptionBoldFont = Font(8.5F, FontStyle.Bold);

    public static Font Font(float size, FontStyle style = FontStyle.Regular)
    {
        if (!Fonts.TryGetValue((size, style), out Font? font))
        {
            font = new Font(FontFamily, size, style, GraphicsUnit.Point);
            Fonts[(size, style)] = font;
        }

        return font;
    }

    private static string ResolveFontFamily()
    {
        using var installed = new InstalledFontCollection();
        HashSet<string> names = installed.Families
            .Select(family => family.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (names.Contains("Segoe UI Variable Text"))
        {
            return "Segoe UI Variable Text";
        }

        return names.Contains("Segoe UI") ? "Segoe UI" : SystemFonts.MessageBoxFont!.FontFamily.Name;
    }
}
