using History.Commons.Enums;

namespace History.WindowsClient.Helpers;

// Display-string lists for the discovery option pickers shared by the bulk management flows.
public static class DiscoveryOptionDisplayHelper
{
    public static string[] GetAllDisplayStrings() =>
    [.. Enum.GetValues<DiscoveryOption>().Select(x => x.ToDisplayString())];

    // Converting posts to a per-user option needs an explicit user list that a bulk conversion
    // cannot supply, so those options are not offered as conversion targets. An optionally
    // excluded option keeps a range from being retargeted to itself.
    public static string[] GetConversionTargetDisplayStrings(DiscoveryOption? excludedOption = null) =>
    [.. Enum.GetValues<DiscoveryOption>().Where(x => x != DiscoveryOption.SelectedUsers && x != DiscoveryOption.UnselectedUsers && x != excludedOption).Select(x => x.ToDisplayString())];
}
