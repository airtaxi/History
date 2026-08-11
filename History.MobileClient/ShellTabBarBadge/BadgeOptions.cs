namespace History.MobileClient.ShellTabBarBadge;

/// <summary>
/// Defines default configuration values for tab bar badges.
/// These values are applied unless overridden when calling <see cref="TabBarBadge.Set"/>.
/// </summary>
public class BadgeOptions
{
    /// <summary>
    /// The default badge style applied when no style is specified in <see cref="TabBarBadge.Set"/>.
    /// </summary>
    public BadgeStyle Style { get; set; } = BadgeStyle.Text;

    /// <summary>
    /// The default text color for badge content (numbers, text, or symbols).
    /// Ignored for dot style.
    /// </summary>
    public Color TextColor { get; set; } = Colors.White;

    /// <summary>
    /// The default background color of the badge.
    /// For text/number badges, this colors the pill background.
    /// For dot badges, this colors the dot itself.
    /// </summary>
    public Color Color { get; set; } = Colors.Red;

    /// <summary>
    /// The default horizontal alignment of the badge relative to its anchor (icon or text).
    /// </summary>
    public HorizontalAlignment HorizontalAlignment { get; set; } = HorizontalAlignment.Right;

    /// <summary>
    /// The default vertical alignment of the badge relative to its anchor (icon or text).
    /// </summary>
    public VerticalAlignment VerticalAlignment { get; set; } = VerticalAlignment.Top;

    /// <summary>
    /// The default font size (in device-independent units) for text badges.
    /// Ignored for dot style.
    /// </summary>
    public double FontSize { get; set; } = 11;

    /// <summary>
    /// The default horizontal offset (in device-independent pixels) applied to badge placement.
    /// </summary>
    public int AnchorX { get; set; }

    /// <summary>
    /// The default vertical offset (in device-independent pixels) applied to badge placement.
    /// </summary>
    public int AnchorY { get; set; }
}
