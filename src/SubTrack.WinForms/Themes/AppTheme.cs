using SubTrack.WinForms.Theme;

namespace SubTrack.WinForms.Themes;

internal static class AppTheme
{
    public static readonly Color WindowBackground = Palette.Canvas;
    public static readonly Color Surface = Palette.Surface;
    public static readonly Color Primary = Palette.PrimaryDeep;
    public static readonly Color PrimarySoft = Palette.PrimarySoft;
    public static readonly Color PrimaryPressed = Palette.PrimaryDeep;
    public static readonly Color NavigationHover = Palette.PrimarySoft;
    public static readonly Color Success = Palette.PrimaryDeep;
    public static readonly Color Warning = Palette.Warning;
    public static readonly Color TextPrimary = Palette.Text;
    public static readonly Color TextSecondary = Palette.Muted;
    public static readonly Color Border = Palette.Border;

    public static readonly Font BrandFont = Palette.Font(16F, FontStyle.Bold);
    public static readonly Font TitleFont = Palette.Font(18F, FontStyle.Bold);
    public static readonly Font SubtitleFont = Palette.Font(10F);
    public static readonly Font SearchFont = Palette.Font(10F);
    public static readonly Font NavigationFont = Palette.Font(9.5F);
    public static readonly Font NavigationActiveFont = Palette.Font(9.5F, FontStyle.Bold);
    public static readonly Font PanelTitleFont = Palette.Font(12F, FontStyle.Bold);
    public static readonly Font ItemBoldFont = Palette.Font(9.5F, FontStyle.Bold);
    public static readonly Font CaptionFont = Palette.Font(8.5F);
    public static readonly Font CaptionBoldFont = Palette.Font(8.5F, FontStyle.Bold);
}
