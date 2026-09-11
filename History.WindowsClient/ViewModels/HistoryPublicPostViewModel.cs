using History.Commons;
using History.Commons.Api.Friendship;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using History.WindowsClient.Models;
using Microsoft.UI.Xaml.Controls;

namespace History.WindowsClient.ViewModels;

// Discover feed post view model: the "..." menu exposes only the moderation,
// report and friend-request actions available for a promoted public post.
public partial class HistoryPublicPostViewModel(PostResponseDto post, BaseViewModel baseViewModel) : HistoryPostViewModel(post, PostType.Discovery, baseViewModel)
{
    public override void PopulateMoreMenuFlyout(MenuFlyout menuFlyout)
    {
        menuFlyout.Items.Clear();

        if (User.UserId == CommonShared.UserId || CommonShared.MyRank >= Rank.Moderator) menuFlyout.Items.Add(Utils.CreateActionItem("게시글 삭제", "\uE74D", DeleteAsync));
        else menuFlyout.Items.Add(Utils.CreateActionItem("게시글 신고", "\uE7C1", PromptAndReportAsync));

        if (User.UserId != CommonShared.UserId && User.Friendship == null) menuFlyout.Items.Add(Utils.CreateActionItem("친구 요청 보내기", "\uE8FA", SendFriendRequestAsync));
    }

    // Asks for the report category before submitting, matching the discover feed's flat action list.
    private async Task PromptAndReportAsync()
    {
        var reportTypes = Enum.GetValues<ReportType>().Select(x => x.ToDisplayString()).ToArray();
        var action = await ShowSelectionDialogAsync("신고 카테고리", reportTypes);
        if (action == null) return;

        await HandleReportAsync(ReportTypeExtensions.FromDisplayString(action));
    }

    private async Task SendFriendRequestAsync()
    {
        var result = await ExecuteRequestAsync(new SendFriendRequest(User.UserId));
        if (!result.IsSuccess) return;

        await RefreshAsync();
        await ShowMessageDialogAsync(new MessageDialogParameters("안내", "친구 요청이 성공적으로 전송되었습니다."));
    }
}
