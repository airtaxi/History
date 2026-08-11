namespace History.MobileClient.ShellTabBarBadge;

/// <summary>
/// Defines the available styles for tab bar badges.
/// </summary>
public enum BadgeStyle
{
    /// <summary>
    /// No badge is displayed (or clears the current badge if one exists).
    /// </summary>
    Hidden,

    /// <summary>
    /// A badge rendered using Unicode text (letters, numbers, symbols, or emoji)
    /// inside a pill-shaped background.
    /// </summary>
    Text,

    /// <summary>
    /// A small colored dot badge. The dot color is controlled by the color parameter.
    /// Ignores text, text size, and text color settings.
    /// </summary>
    Dot
}
