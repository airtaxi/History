namespace History.MobileClient.Helpers;

/// <summary>
/// Applies a count to a pill badge. The badge is hidden when the count is zero
/// and capped at "99+" so the badge never grows past the pill corner.
/// </summary>
public static class PillBadgeHelper
{
    public static void Apply(Border badgeBorder, Label badgeLabel, int count)
    {
        badgeBorder.IsVisible = count > 0;
        badgeLabel.Text = count > 99 ? "99+" : count.ToString();
    }
}
