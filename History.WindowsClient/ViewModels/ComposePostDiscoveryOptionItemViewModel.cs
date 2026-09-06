using CommunityToolkit.Mvvm.ComponentModel;
using History.Commons.Enums;
using History.WindowsClient.Helpers;

namespace History.WindowsClient.ViewModels;

// Single discovery-option choice for the compose post discovery combo box. Carries the
// option value, its display string, and the same Segoe Fluent glyph the post list uses.
public sealed class ComposePostDiscoveryOptionItemViewModel(DiscoveryOption option)
{
    public DiscoveryOption Option { get; } = option;

    public string DisplayText { get; } = option.ToDisplayString();

    public string Glyph { get; } = PostHelper.GetDiscoveryOptionGlyph(option);
}
