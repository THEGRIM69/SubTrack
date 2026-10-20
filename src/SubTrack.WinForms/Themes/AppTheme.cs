namespace SubTrack.WinForms.Themes;

internal static class AppTheme
{
    public static readonly Color WindowBackground = Color.FromArgb(245, 247, 250);
    public static readonly Color Surface = Color.White;
    public static readonly Color Primary = Color.FromArgb(43, 91, 215);
    public static readonly Color PrimarySoft = Color.FromArgb(232, 238, 253);
    public static readonly Color TextPrimary = Color.FromArgb(29, 36, 51);
    public static readonly Color TextSecondary = Color.FromArgb(101, 110, 128);
    public static readonly Color Border = Color.FromArgb(222, 226, 234);

    public static readonly Font TitleFont = new("Segoe UI", 22F, FontStyle.Bold);
    public static readonly Font SubtitleFont = new("Segoe UI", 10.5F, FontStyle.Regular);
    public static readonly Font NavigationFont = new("Segoe UI", 9F, FontStyle.Regular);
}
