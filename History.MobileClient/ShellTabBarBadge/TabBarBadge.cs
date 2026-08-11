namespace History.MobileClient.ShellTabBarBadge;

/// <summary>
/// Provides a cross-platform API for showing and managing badges on Shell tab bar items.
/// </summary>
public static partial class TabBarBadge
{
    private static BadgeOptions s_options = new();

    /// <summary>
    /// Applies global default options for all badges in the application.
    /// </summary>
    public static void Configure(BadgeOptions options) => s_options = options;

    /// <summary>
    /// Sets or updates a badge on the specified Shell tab bar item.
    /// </summary>
    public static void Set(
        int tabIndex,
        string text = null,
        Color textColor = null,
        Color color = null,
        int? anchorX = null,
        int? anchorY = null,
        BadgeStyle? style = null,
        HorizontalAlignment? horizontal = null,
        VerticalAlignment? vertical = null,
        double? fontSize = null)
    {
        if (style == BadgeStyle.Hidden)
        {
            HideImpl(tabIndex);
            return;
        }

        var finalStyle = style ?? s_options.Style;
        var isDot = finalStyle == BadgeStyle.Dot;

        ShowImpl(
            tabIndex,
            isDot,
            isDot ? null : text,
            isDot ? Colors.Transparent : (textColor ?? s_options.TextColor),
            color ?? s_options.Color,
            anchorX ?? s_options.AnchorX,
            anchorY ?? s_options.AnchorY,
            horizontal ?? s_options.HorizontalAlignment,
            vertical ?? s_options.VerticalAlignment,
            fontSize ?? s_options.FontSize);
    }

    static partial void ShowImpl(
        int tabIndex,
        bool isDot,
        string text,
        Color textColor,
        Color color,
        int anchorX,
        int anchorY,
        HorizontalAlignment horizontal,
        VerticalAlignment vertical,
        double fontSize);

    static partial void HideImpl(int tabIndex);
}
