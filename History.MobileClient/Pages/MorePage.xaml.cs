using History.Commons.Enums;
using History.Commons.Api.Post;
using History.MobileClient.Helpers;
using History.MobileClient.KakaoStory;

namespace History.MobileClient.Pages;

public partial class MorePage : ContentPage
{
    public MorePage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        var safeAreaTopHeight = LayoutHelper.GetSafeAreaTopHeight();
        if (safeAreaTopHeight != 0)
        {
            var statusBarHeight = LayoutHelper.GetStatusBarHeight();
            Padding = new Thickness(Padding.Left, -(safeAreaTopHeight - statusBarHeight), Padding.Right, Padding.Bottom);
        }

        BulkPostManagementDivider.IsVisible = true;
        BulkPostManagementGrid.IsVisible = true;

        KakaoStoryExtrasDivider.IsVisible = true;
        KakaoStoryExtrasGrid.IsVisible = true;

        var isModerator = Shared.MyRank >= Rank.Moderator;
        ModerationDivider.IsVisible = isModerator;
        ModerationRecordsGrid.IsVisible = isModerator;
        InviteCodeRequestDivider.IsVisible = isModerator;
        InviteCodeRequestsGrid.IsVisible = isModerator;
    }

    private async void OnBookmarkedPostsGridTapped(object sender, TappedEventArgs e)
    {
        var page = new BlazorBookmarkedPostsPage();
        await App.PushAsync(page);
    }

    private async void OnPublicPostGridTapped(object sender, TappedEventArgs e)
    {
        var page = new BlazorPublicPostsPage();
        await App.PushAsync(page);
    }

    private async void OnStickersGridTapped(object sender, TappedEventArgs e)
    {
        var page = new StickersPage();
        await App.PushAsync(page);
    }

    private async void OnModerationRecordsGridTapped(object sender, TappedEventArgs e)
    {
        var page = new ModerationRecordsPage();
        await App.PushAsync(page);
    }

    private async void OnInviteCodesGridTapped(object sender, TappedEventArgs e)
    {
        var page = new InviteCodesPage();
        await App.PushAsync(page);
    }

    private async void OnInviteCodeRequestsGridTapped(object sender, TappedEventArgs e)
    {
        var page = new InviteCodeRequestsPage();
        await App.PushAsync(page);
    }

    private async void OnBulkPostManagementGridTapped(object sender, TappedEventArgs e)
    {
        var action = await App.Page.DisplayActionSheetAsync(
            "게시글 일괄 관리",
            Constants.PromptCancel,
            null,
            "원하는 게시글만 선택해 변경/삭제",
            "특정 공개 범위를 다른 범위로 일괄 전환",
            "특정 공개 범위를 가진 글을 전부 삭제",
            "모든 글을 특정 공개 범위로 전환",
            "모든 글 삭제");

        if (action == null || action == Constants.PromptCancel) return;

        if (action == "원하는 게시글만 선택해 변경/삭제")
        {
            var page = new BulkPostManagePage();
            await App.PushAsync(page);
        }
        else if (action == "특정 공개 범위를 다른 범위로 일괄 전환") await ChangeDiscoveryOptionByFilterAsync();
        else if (action == "특정 공개 범위를 가진 글을 전부 삭제") await DeletePostsByFilterAsync();
        else if (action == "모든 글을 특정 공개 범위로 전환") await ChangeAllDiscoveryOptionsAsync();
        else if (action == "모든 글 삭제") await DeleteAllPostsAsync();
    }

    private async void OnKakaoStoryExtrasGridTapped(object sender, TappedEventArgs e)
    {
        if (!await KakaoStoryUtils.EnsureLoggedInAsync(this)) return;

        var page = new KakaoStoryExtrasPage();
        await App.PushModalAsync(page);
    }

    private static string[] GetDiscoveryOptionDisplayStrings() =>
        Enum.GetValues<DiscoveryOption>().Select(x => x.ToDisplayString()).ToArray();

    private async Task ChangeDiscoveryOptionByFilterAsync()
    {
        var displayStrings = GetDiscoveryOptionDisplayStrings();

        var rawFrom = await App.Page.DisplayActionSheetAsync("현재 공개 범위 선택", Constants.PromptCancel, null, displayStrings);
        if (rawFrom == null || rawFrom == Constants.PromptCancel) return;

        var rawTo = await App.Page.DisplayActionSheetAsync("변경할 공개 범위 선택", Constants.PromptCancel, null, displayStrings);
        if (rawTo == null || rawTo == Constants.PromptCancel) return;

        var from = DiscoveryOptionExtensions.FromDisplayString(rawFrom);
        var to = DiscoveryOptionExtensions.FromDisplayString(rawTo);
        if (from == to) return;

        var confirm = await App.Page.DisplayAlertAsync(
            "확인",
            $"공개 범위가 '{from.ToDisplayString()}'인 게시글을 '{to.ToDisplayString()}'로 일괄 변경하시겠습니까?",
            Constants.PromptOk,
            Constants.PromptCancel);
        if (!confirm) return;

        var result = await App.ExecuteRequestAsync(new BulkChangeDiscoveryOption(from, to));
        if (result.IsSuccess)
        {
            InvalidatePostPages();
            await App.Page.DisplayAlertAsync("완료", "일괄 변경이 완료되었습니다.", Constants.PromptOk);
        }
    }

    private async Task DeletePostsByFilterAsync()
    {
        var displayStrings = GetDiscoveryOptionDisplayStrings();

        var rawPermission = await App.Page.DisplayActionSheetAsync("삭제할 공개 범위 선택", Constants.PromptCancel, null, displayStrings);
        if (rawPermission == null || rawPermission == Constants.PromptCancel) return;

        var permission = DiscoveryOptionExtensions.FromDisplayString(rawPermission);

        var confirm = await App.Page.DisplayAlertAsync(
            "확인",
            $"공개 범위가 '{permission.ToDisplayString()}'인 게시글을 모두 삭제하시겠습니까? 되돌릴 수 없습니다.",
            Constants.PromptOk,
            Constants.PromptCancel);
        if (!confirm) return;

        var result = await App.ExecuteRequestAsync(new BulkDeletePosts(discoveryOption: permission));
        if (result.IsSuccess)
        {
            InvalidatePostPages();
            await App.Page.DisplayAlertAsync("완료", "일괄 삭제가 완료되었습니다.", Constants.PromptOk);
        }
    }

    private async Task ChangeAllDiscoveryOptionsAsync()
    {
        var displayStrings = GetDiscoveryOptionDisplayStrings();

        var rawTo = await App.Page.DisplayActionSheetAsync("변경할 공개 범위 선택", Constants.PromptCancel, null, displayStrings);
        if (rawTo == null || rawTo == Constants.PromptCancel) return;

        var to = DiscoveryOptionExtensions.FromDisplayString(rawTo);

        var confirm = await App.Page.DisplayAlertAsync(
            "확인",
            $"모든 게시글의 공개 범위를 '{to.ToDisplayString()}'로 변경하시겠습니까?",
            Constants.PromptOk,
            Constants.PromptCancel);
        if (!confirm) return;

        var result = await App.ExecuteRequestAsync(new BulkChangeDiscoveryOption(null, to));
        if (result.IsSuccess)
        {
            InvalidatePostPages();
            await App.Page.DisplayAlertAsync("완료", "일괄 변경이 완료되었습니다.", Constants.PromptOk);
        }
    }

    private async Task DeleteAllPostsAsync()
    {
        var confirm = await App.Page.DisplayAlertAsync(
            "확인",
            "모든 게시글을 삭제하시겠습니까? 되돌릴 수 없습니다.",
            "삭제",
            Constants.PromptCancel);
        if (!confirm) return;

        var result = await App.ExecuteRequestAsync(new BulkDeletePosts());
        if (result.IsSuccess)
        {
            InvalidatePostPages();
            await App.Page.DisplayAlertAsync("완료", "일괄 삭제가 완료되었습니다.", Constants.PromptOk);
        }
    }

    private static void InvalidatePostPages()
    {
        TimelinePage.ShouldRefresh = true;
        UserPage.ShouldRefresh = true;
        PublicPostsPage.ShouldRefresh = true;
    }
}
