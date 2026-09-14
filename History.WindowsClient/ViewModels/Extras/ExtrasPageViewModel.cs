using CommunityToolkit.Mvvm.Input;
using History.Commons;
using History.Commons.Enums;
using History.WindowsClient.Helpers;
using History.WindowsClient.Models;
using History.WindowsClient.Pages.Extras;
using Microsoft.UI.Xaml;

namespace History.WindowsClient.ViewModels.Extras;

// Extras page state: resolves which extra-feature entries the signed-in account can see.
// Entries whose target page is not implemented yet answer with an under-implementation notice.
public sealed partial class ExtrasPageViewModel : BaseViewModel
{
    private const string UnderImplementationTitle = "안내";
    private const string UnderImplementationMessage = "구현 중인 기능입니다.";

    public Visibility InviteCodeRequestsVisibility { get; } = CommonShared.MyRank >= Rank.Moderator ? Visibility.Visible : Visibility.Collapsed;

    public Visibility ModerationRecordsVisibility { get; } = CommonShared.MyRank >= Rank.Moderator ? Visibility.Visible : Visibility.Collapsed;

    public Visibility KakaoStoryExtrasVisibility { get; } = KakaoStoryFeatureGateHelper.IsEnabled ? Visibility.Visible : Visibility.Collapsed;

    [RelayCommand]
    private void OpenStickers() => RequestNavigation(typeof(StickersPage));

    [RelayCommand]
    private void OpenInviteCodes() => RequestNavigation(typeof(InviteCodesPage));

    [RelayCommand]
    private void OpenInviteCodeRequests() => RequestNavigation(typeof(InviteCodeRequestsPage));

    // TODO: Open the bulk post management page once it exists.
    [RelayCommand]
    private async Task OpenBulkPostManagementAsync() => await ShowUnderImplementationMessageAsync();

    // TODO: Navigate to the Kakao Story extras page once it exists.
    [RelayCommand]
    private async Task OpenKakaoStoryExtrasAsync() => await ShowUnderImplementationMessageAsync();

    // TODO: Open the moderation records page once it exists.
    [RelayCommand]
    private async Task OpenModerationRecordsAsync() => await ShowUnderImplementationMessageAsync();

    // TODO: Remove this helper once every entry above is implemented.
    private async Task ShowUnderImplementationMessageAsync() => await ShowMessageDialogAsync(new MessageDialogParameters(UnderImplementationTitle, UnderImplementationMessage));
}
