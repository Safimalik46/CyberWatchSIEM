namespace CyberWatchSIEM.Utils;

public static class ThemeColors
{
    public static Color Background = Color.FromArgb(15, 23, 42);
    public static Color Card = Color.FromArgb(30, 41, 59);
    public static Color Primary = Color.FromArgb(56, 189, 248);
    public static Color Success = Color.FromArgb(34, 197, 94);
    public static Color Warning = Color.FromArgb(245, 158, 11);
    public static Color Danger = Color.FromArgb(239, 68, 68);
    public static Color Text = Color.White;
    public static Color TextMuted = Color.FromArgb(148, 163, 184);
    public static Color Sidebar = Color.FromArgb(15, 23, 42);
    public static Color Border = Color.FromArgb(51, 65, 85);

    public static void ApplyDarkTheme()
    {
        Background = Color.FromArgb(15, 23, 42);
        Card = Color.FromArgb(30, 41, 59);
        Text = Color.White;
        TextMuted = Color.FromArgb(148, 163, 184);
        Sidebar = Color.FromArgb(15, 23, 42);
    }

    public static void ApplyLightTheme()
    {
        Background = Color.FromArgb(241, 245, 249);
        Card = Color.White;
        Text = Color.FromArgb(15, 23, 42);
        TextMuted = Color.FromArgb(100, 116, 139);
        Sidebar = Color.FromArgb(226, 232, 240);
    }
}
