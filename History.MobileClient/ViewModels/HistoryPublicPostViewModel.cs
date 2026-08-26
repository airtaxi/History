using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using History.Commons.Api.Friendship;
using History.Commons.Api.Moderation;
using History.Commons.Api.Post;
using History.Commons.Api.Report;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using History.MobileClient.Messages;
using History.Commons.Enums;
using History.Commons;

namespace History.MobileClient.ViewModels;

public partial class HistoryPublicPostViewModel(PostResponseDto post) : HistoryPostViewModel(post, PostType.Discovery)
{
    [RelayCommand]
    public async Task HandlePublicPostMoreTapAsync()
    {
        var options = new List<string>();

        if (User.UserId == CommonShared.UserId) options.Add("게시글 삭제");
        else if (CommonShared.MyRank >= Rank.Moderator) options.Add("게시글 삭제");
        else options.AddRange("게시글 신고");

        var canSendFriendRequest = User.UserId != CommonShared.UserId && User.Friendship == null;
        if (canSendFriendRequest) options.Add("친구 요청 보내기");

        var action = await App.Page.DisplayActionSheetAsync("홍보 게시글 옵션", Constants.PromptCancel, null, [.. options]);
        if (action == null || action == Constants.PromptCancel) return;

        if (action == "게시글 삭제") await DeleteAsync();
        else if (action == "게시글 신고")
        {
            var reportTypes = Enum.GetValues<ReportType>().Select(x => x.ToDisplayString()).ToArray();

            var rawReportType = await App.Page.DisplayActionSheetAsync("신고 카테고리", Constants.PromptCancel, null, reportTypes);
            if (rawReportType == null || rawReportType == Constants.PromptCancel) return;
            var reportType = ReportTypeExtensions.FromDisplayString(rawReportType);

            var result = await App.ExecuteRequestAsync(new CreateReportRecord(new()
            {
                Type = reportType,
                Target = ReportTarget.Post,
                AssociatedId = Post.Id
            }));

            if (result.IsSuccess) await App.Page.DisplayAlertAsync("안내", "게시글 신고가 성공적으로 전송되었습니다. 관리자 검토 후 처리 예정입니다.", Constants.PromptOk);
        }
        else if (action == "친구 요청 보내기")
        {
            var result = await App.ExecuteRequestAsync(new SendFriendRequest(User.UserId));
            if (result.IsSuccess)
            {
                await RefreshAsync();
                await App.Page.DisplayAlertAsync("안내", "친구 요청이 성공적으로 전송되었습니다.", Constants.PromptOk);
            }
        }
    }

    private async Task DeleteAsync()
    {
        if (User.UserId != CommonShared.UserId)
        {
            if (CommonShared.MyRank < Rank.Moderator)
            {
                await App.Page.DisplayAlertAsync("권한 부족", "게시글을 삭제할 권한이 없습니다.", Constants.PromptOk);
                return;
            }

            var reportTypes = Enum.GetValues<ReportType>().Select(x => x.ToDisplayString()).ToArray();
            var action = await App.Page.DisplayActionSheetAsync("제재 카테고리 선택", Constants.PromptCancel, null, reportTypes);
            if (action == null || action == Constants.PromptCancel) return;
            var reportType = ReportTypeExtensions.FromDisplayString(action);

            var reason = await App.Page.DisplayPromptAsync("게시글 삭제", "게시글을 삭제하는 이유를 입력해주세요.", "삭제", "취소", "삭제 사유");
            if (string.IsNullOrWhiteSpace(reason)) return;

            var deleteResult = await App.ExecuteRequestAsync(new ModerationDeletePost(Post.Id, reason, reportType));
            if (deleteResult.IsSuccess) WeakReferenceMessenger.Default.Send(new ValueDeletedMessage<PostResponseDto>(Post));
        }
        else
        {
            var confirm = await App.Page.DisplayAlertAsync("게시글 삭제", "정말로 게시글을 삭제하시겠습니까? 홍보 게시글만 삭제되는 것이 아닌 홍보 게시글과 함께 원본 게시글까지 완전히 삭제됩니다. 되돌릴 수 없으니 주의하세요.", Constants.PromptOk, Constants.PromptCancel);
            if (confirm)
            {
                var deleteResult = await App.ExecuteRequestAsync(new DeletePost(Post.Id));
                if (deleteResult.IsSuccess) WeakReferenceMessenger.Default.Send(new ValueDeletedMessage<PostResponseDto>(Post));
            }
        }
    }
}
