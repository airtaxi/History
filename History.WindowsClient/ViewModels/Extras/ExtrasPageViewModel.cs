using CommunityToolkit.Mvvm.Input;
using History.Commons;
using History.Commons.Api.Post;
using History.Commons.Enums;
using History.WindowsClient.Helpers;
using History.WindowsClient.Messages;
using History.WindowsClient.Models;
using History.WindowsClient.Pages.Extras;
using History.WindowsClient.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace History.WindowsClient.ViewModels.Extras;

// Extras page state: resolves which extra-feature entries the signed-in account can see.
// Entries whose target page is not implemented yet answer with an under-implementation notice.
public sealed partial class ExtrasPageViewModel : BaseViewModel
{
    private const string UnderImplementationTitle = "안내";
    private const string UnderImplementationMessage = "구현 중인 기능입니다.";

    private const string SelectPostsText = "원하는 게시글만 선택해 변경/삭제";
    private const string ChangeByFilterText = "특정 공개 범위를 다른 범위로 일괄 전환";
    private const string DeleteByFilterText = "특정 공개 범위를 가진 글을 전부 삭제";
    private const string ChangeAllText = "모든 글을 특정 공개 범위로 전환";
    private const string DeleteAllText = "모든 글 삭제";

    public Visibility InviteCodeRequestsVisibility { get; } = CommonShared.MyRank >= Rank.Moderator ? Visibility.Visible : Visibility.Collapsed;

    public Visibility ModerationRecordsVisibility { get; } = CommonShared.MyRank >= Rank.Moderator ? Visibility.Visible : Visibility.Collapsed;

    public Visibility KakaoStoryExtrasVisibility { get; } = KakaoStoryFeatureGateHelper.IsEnabled ? Visibility.Visible : Visibility.Collapsed;

    [RelayCommand]
    private void OpenStickers() => RequestNavigation(typeof(StickersPage));

    [RelayCommand]
    private void OpenInviteCodes() => RequestNavigation(typeof(InviteCodesPage));

    [RelayCommand]
    private void OpenInviteCodeRequests() => RequestNavigation(typeof(InviteCodeRequestsPage));

    // Offers the bulk operations for the signed-in account's own posts. The selection-based
    // operation opens the dedicated post picker page.
    [RelayCommand]
    private async Task OpenBulkPostManagementAsync()
    {
        var action = await ShowSelectionDialogAsync("게시글 일괄 관리", [SelectPostsText, ChangeByFilterText, DeleteByFilterText, ChangeAllText, DeleteAllText]);
        if (action == null) return;

        if (action == SelectPostsText) RequestNavigation(typeof(BulkPostManagePage));
        else if (action == ChangeByFilterText) await ChangeDiscoveryOptionByFilterAsync();
        else if (action == DeleteByFilterText) await DeletePostsByFilterAsync();
        else if (action == ChangeAllText) await ChangeAllDiscoveryOptionsAsync();
        else if (action == DeleteAllText) await DeleteAllPostsAsync();
    }

    // TODO: Navigate to the Kakao Story extras page once it exists.
    [RelayCommand]
    private async Task OpenKakaoStoryExtrasAsync() => await ShowUnderImplementationMessageAsync();

    [RelayCommand]
    private void OpenModerationRecords() => RequestNavigation(typeof(ModerationRecordsPage));

    // Retargets every post that currently has one discovery option to another option.
    private async Task ChangeDiscoveryOptionByFilterAsync()
    {
        var rawFrom = await ShowSelectionDialogAsync("현재 공개 범위 선택", DiscoveryOptionDisplayHelper.GetAllDisplayStrings());
        if (rawFrom == null) return;

        var from = DiscoveryOptionExtensions.FromDisplayString(rawFrom);

        var rawTo = await ShowSelectionDialogAsync("변경할 공개 범위 선택", DiscoveryOptionDisplayHelper.GetConversionTargetDisplayStrings(from));
        if (rawTo == null) return;

        var to = DiscoveryOptionExtensions.FromDisplayString(rawTo);

        var confirm = await ShowMessageDialogAsync(new MessageDialogParameters("확인", $"공개 범위가 '{from.ToDisplayString()}'인 게시글을 '{to.ToDisplayString()}'로 일괄 변경하시겠습니까?", DialogHelper.DefaultOkButtonText, DialogHelper.DefaultCancelButtonText));
        if (confirm != ContentDialogResult.Primary) return;

        var result = await ExecuteRequestAsync(new BulkChangeDiscoveryOption(from, to));
        if (result.IsSuccess)
        {
            RefreshPostPages();
            await ShowMessageDialogAsync(new MessageDialogParameters("완료", "일괄 변경이 완료되었습니다."));
        }
    }

    // Deletes every post that has the chosen discovery option.
    private async Task DeletePostsByFilterAsync()
    {
        var rawPermission = await ShowSelectionDialogAsync("삭제할 공개 범위 선택", DiscoveryOptionDisplayHelper.GetAllDisplayStrings());
        if (rawPermission == null) return;

        var permission = DiscoveryOptionExtensions.FromDisplayString(rawPermission);

        var confirm = await ShowMessageDialogAsync(new MessageDialogParameters("확인", $"공개 범위가 '{permission.ToDisplayString()}'인 게시글을 모두 삭제하시겠습니까? 되돌릴 수 없습니다.", DialogHelper.DefaultOkButtonText, DialogHelper.DefaultCancelButtonText));
        if (confirm != ContentDialogResult.Primary) return;

        var result = await ExecuteRequestAsync(new BulkDeletePosts(discoveryOption: permission));
        if (result.IsSuccess)
        {
            RefreshPostPages();
            await ShowMessageDialogAsync(new MessageDialogParameters("완료", "일괄 삭제가 완료되었습니다."));
        }
    }

    // Retargets every post regardless of its current discovery option.
    private async Task ChangeAllDiscoveryOptionsAsync()
    {
        var rawTo = await ShowSelectionDialogAsync("변경할 공개 범위 선택", DiscoveryOptionDisplayHelper.GetConversionTargetDisplayStrings());
        if (rawTo == null) return;

        var to = DiscoveryOptionExtensions.FromDisplayString(rawTo);

        var confirm = await ShowMessageDialogAsync(new MessageDialogParameters("확인", $"모든 게시글의 공개 범위를 '{to.ToDisplayString()}'로 변경하시겠습니까?", DialogHelper.DefaultOkButtonText, DialogHelper.DefaultCancelButtonText));
        if (confirm != ContentDialogResult.Primary) return;

        var result = await ExecuteRequestAsync(new BulkChangeDiscoveryOption(null, to));
        if (result.IsSuccess)
        {
            RefreshPostPages();
            await ShowMessageDialogAsync(new MessageDialogParameters("완료", "일괄 변경이 완료되었습니다."));
        }
    }

    // Deletes every post of the signed-in account without any filter.
    private async Task DeleteAllPostsAsync()
    {
        var confirm = await ShowMessageDialogAsync(new MessageDialogParameters("확인", "모든 게시글을 삭제하시겠습니까? 되돌릴 수 없습니다.", "삭제", DialogHelper.DefaultCancelButtonText));
        if (confirm != ContentDialogResult.Primary) return;

        var result = await ExecuteRequestAsync(new BulkDeletePosts());
        if (result.IsSuccess)
        {
            RefreshPostPages();
            await ShowMessageDialogAsync(new MessageDialogParameters("완료", "일괄 삭제가 완료되었습니다."));
        }
    }

    // Bulk operations replace or remove many posts at once, so the main window's post surfaces
    // are asked to reload instead of receiving per-post change messages.
    private static void RefreshPostPages() => RefreshRequestedMessage.Send(MainWindow.Frame.XamlRoot);

    // TODO: Remove this helper once every entry above is implemented.
    private async Task ShowUnderImplementationMessageAsync() => await ShowMessageDialogAsync(new MessageDialogParameters(UnderImplementationTitle, UnderImplementationMessage));
}
