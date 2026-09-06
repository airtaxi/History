using CommunityToolkit.Mvvm.ComponentModel;
using History.Commons.Enums;
using History.WindowsClient.Helpers;
using Microsoft.UI.Xaml;

namespace History.WindowsClient.ViewModels;

// Single comment-permission choice for the compose post options surface. An option is
// selectable only when its audience is not wider than the post's discovery option: the
// discovery option ordering matches the audience scope, so a scope comparison is enough.
public sealed partial class ComposePostCommentPermissionItemViewModel(AccessPermission? permission) : ObservableObject
{
    public AccessPermission? Permission { get; } = permission;

    public string DisplayText { get; } = permission?.ToDisplayString() ?? "설정 안 함";

    // The not-set entry has no audience scope, so it keeps a neutral marker glyph.
    public string Glyph { get; } = permission == null ? "\uE9CE" : PostHelper.GetDiscoveryOptionGlyph(permission.Value.ToDiscoveryOption());

    [ObservableProperty]
    public partial bool IsEnabled { get; set; } = true;

    // Recomputes the availability from the given discovery option. Selected-user scopes
    // forbid setting a comment permission at all: every non-null permission is disabled so
    // only the not-set entry stays selectable.
    public void UpdateAvailability(DiscoveryOption discoveryOption)
    {
        if (Permission == null)
        {
            IsEnabled = true;
            return;
        }

        if (discoveryOption is DiscoveryOption.SelectedUsers or DiscoveryOption.UnselectedUsers)
        {
            IsEnabled = false;
            return;
        }

        IsEnabled = Permission.Value.ToDiscoveryOption() <= discoveryOption;
    }
}
