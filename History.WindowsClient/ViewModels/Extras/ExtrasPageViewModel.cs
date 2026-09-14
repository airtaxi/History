using CommunityToolkit.Mvvm.Input;
using History.Commons;
using History.Commons.Enums;
using History.WindowsClient.Helpers;
using History.WindowsClient.Models;
using Microsoft.UI.Xaml;

namespace History.WindowsClient.ViewModels.Extras;

// Extras page state: resolves which extra-feature entries the signed-in account can see.
// Every entry currently answers with an under-implementation notice until its target page
// is implemented, so the page can be reviewed as pure UI first.
public sealed partial class ExtrasPageViewModel : BaseViewModel
{
    private const string UnderImplementationTitle = "안내";
    private const string UnderImplementationMessage = "구현 중인 기능입니다.";

    public Visibility InviteCodeRequestsVisibility { get; } = CommonShared.MyRank >= Rank.Moderator ? Visibility.Visible : Visibility.Collapsed;

    public Visibility ModerationRecordsVisibility { get; } = CommonShared.MyRank >= Rank.Moderator ? Visibility.Visible : Visibility.Collapsed;

    public Visibility KakaoStoryExtrasVisibility { get; } = KakaoStoryFeatureGateHelper.IsEnabled ? Visibility.Visible : Visibility.Collapsed;

    // TODO: Open the sticker browse page once it exists.
    [RelayCommand]
    private async Task OpenStickersAsync() => await ShowUnderImplementationMessageAsync();

    // TODO: Open the invite code page once it exists.
    [RelayCommand]
    private async Task OpenInviteCodesAsync() => await ShowUnderImplementationMessageAsync();

    // TODO: Open the invite code request management page once it exists.
    [RelayCommand]
    private async Task OpenInviteCodeRequestsAsync() => await ShowUnderImplementationMessageAsync();

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
