using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using History.Commons;
using History.Commons.Api.Comment;
using History.Commons.Api.Moderation;
using History.Commons.Api.Report;
using History.Commons.DataTypes.Contents;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using History.WindowsClient.Helpers;
using History.WindowsClient.Messages;
using History.WindowsClient.Models;
using History.WindowsClient.Pages;
using History.WindowsClient.Views;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace History.WindowsClient.ViewModels;

// Dialog prompts are requested on the parent post's base view model; the "..."
// menu is populated by PopulateMoreMenuFlyout with the action labels as item Tag values.
public partial class HistoryCommentViewModel : BaseCommentViewModel, IRecipient<ValueChangedMessage<CommentResponseDto>>
{
    [ObservableProperty]
    public partial CommentResponseDto Comment { get; private set; }

    // Comment-dependent properties — set in UpdateComment.
    [ObservableProperty]
    public partial bool IsMyComment { get; private set; }

    public HistoryCommentViewModel(CommentResponseDto comment, bool isMyPost, PostType postType, BasePostViewModel parentViewModel) : base(isMyPost, postType, parentViewModel)
    {
        UpdateComment(comment);

        WeakReferenceMessenger.Default.Register(this);
    }

    public void Receive(ValueChangedMessage<CommentResponseDto> message)
    {
        if (message.Value.Id != Comment.Id) return;

        UpdateComment(message.Value);
    }

    private void UpdateComment(CommentResponseDto comment)
    {
        try
        {
            // Compute all derived properties from the new comment before assigning Comment.
            var user = comment.User;

            Nickname = user.Nickname;
            IsModerator = user.Rank == Rank.Moderator;
            IsAdmin = user.Rank == Rank.Admin;
            ProfileThumbnailImageSource = user.ProfileThumbnailMediaId == null ? null : new BitmapImage(new Uri(CommonUtils.GenerateMediaUri(user.ProfileThumbnailMediaId)));

            IsMyComment = user.UserId == CommonShared.UserId;
            HasLikes = comment.LikedUsers.Count > 0;
            LikesCount = comment.LikedUsers.Count;
            Liked = comment.LikedUsers.Any(x => x.UserId == CommonShared.UserId);

            Contents = PostHelper.GenerateContentViewModels(comment.Contents, PostType);

            CreatedAt = comment.CreatedAt;
            ModifiedAt = comment.ModifiedAt;
            TimestampText = PostHelper.GenerateFriendlyTimestamp(CreatedAt, ModifiedAt);

            // Assign Comment last so all derived properties are already up-to-date.
            Comment = comment;
        }
        catch (Exception) { } // Ignore any exceptions during update, as the view might be in the foreground.
    }

    public override void PopulateMoreMenuFlyout(MenuFlyout menuFlyout)
    {
        menuFlyout.Items.Clear();

        var likeGlyph = Liked ? "\uEA92" : "\uEB52";
        menuFlyout.Items.Add(Utils.CreateActionItem(Liked ? "좋아요 취소" : "좋아요", likeGlyph, HandleLikeAsync));

        if (IsMyComment) menuFlyout.Items.Add(Utils.CreateActionItem("댓글 수정", "\uE70F", HandleEditComment));
        if (IsMyComment || IsMyPost || CommonShared.MyRank >= Rank.Moderator) menuFlyout.Items.Add(Utils.CreateActionItem("댓글 삭제", "\uE74D", DeleteAsync));

        if (!IsMyComment && CommonShared.MyRank < Rank.Moderator)
        {
            var reportSubItem = new MenuFlyoutSubItem { Text = "댓글 신고" };
            foreach (var reportType in Enum.GetValues<ReportType>())
            {
                var reportTypeValue = reportType;
                reportSubItem.Items.Add(Utils.CreateActionItem(reportType.ToDisplayString(), "\uE7C1", () => HandleReportAsync(reportTypeValue)));
            }
            menuFlyout.Items.Add(reportSubItem);
        }
    }

    // Opens the comment edit window; the window closes itself after a successful edit.
    private void HandleEditComment() => new EditCommentWindow(new EditCommentWindowViewModel(Comment));

    private async Task HandleReportAsync(ReportType reportType)
    {
        var result = await ParentViewModel.BaseViewModel.ExecuteRequestAsync(new CreateReportRecord(new()
        {
            Type = reportType,
            Target = ReportTarget.Comment,
            AssociatedId = Comment.Id
        }));

        if (result.IsSuccess) await ParentViewModel.BaseViewModel.ShowMessageDialogAsync(new MessageDialogParameters("안내", "댓글 신고가 성공적으로 전송되었습니다. 관리자 검토 후 처리 예정입니다."));
    }

    public override async Task HandleCommentLikeTapAsync()
    {
        var users = Comment.LikedUsers;
        if (users.Count == 0)
        {
            await ParentViewModel.BaseViewModel.ShowMessageDialogAsync(new MessageDialogParameters("오류", "이 댓글에 좋아요를 누른 사용자가 없습니다."));
            return;
        }

        // TODO: Navigate to the interactions page once it is implemented.
    }

    public override async Task HandleLikeAsync()
    {
        var commentResult = await ParentViewModel.BaseViewModel.ExecuteRequestAsync(new HandleCommentLike(Comment.Id));
        if (commentResult.IsSuccess) WeakReferenceMessenger.Default.Send(new ValueChangedMessage<CommentResponseDto>(commentResult.Value));
    }

    public override async Task DeleteAsync()
    {
        if (Comment.User.UserId != CommonShared.UserId && CommonShared.MyRank >= Rank.Moderator)
        {
            var reportTypes = Enum.GetValues<ReportType>().Select(x => x.ToDisplayString()).ToArray();
            var action = await ShowSelectionDialogAsync("제재 카테고리 선택", reportTypes);
            if (action == null) return;
            var reportType = ReportTypeExtensions.FromDisplayString(action);

            var reason = await ShowInputDialogAsync(new InputDialogParameters("댓글 삭제", "댓글을 삭제하는 이유를 입력해주세요."));
            if (string.IsNullOrWhiteSpace(reason)) return;

            var deleteResult = await ParentViewModel.BaseViewModel.ExecuteRequestAsync(new ModerationDeleteComment(Comment.Id, reason, reportType));
            if (deleteResult.IsSuccess) WeakReferenceMessenger.Default.Send(new ValueDeletedMessage<CommentResponseDto>(Comment));
        }
        else if (IsMyPost || Comment.User.UserId == CommonShared.UserId)
        {
            var confirm = await ParentViewModel.BaseViewModel.ShowMessageDialogAsync(new MessageDialogParameters("댓글 삭제", "정말로 댓글을 삭제하시겠습니까?", "삭제", "취소"));
            if (confirm != ContentDialogResult.Primary) return;

            var deleteResult = await ParentViewModel.BaseViewModel.ExecuteRequestAsync(new DeleteComment(Comment.Id));
            if (deleteResult.IsSuccess) WeakReferenceMessenger.Default.Send(new ValueDeletedMessage<CommentResponseDto>(Comment));
        }
        else await ParentViewModel.BaseViewModel.ShowMessageDialogAsync(new MessageDialogParameters("권한 부족", "댓글을 삭제할 권한이 없습니다."));
    }

    public override async Task HandleTapAsync()
    {
        // The comment editor listens for comment taps in the unwrapped post view (not implemented yet).
        if (PostType == PostType.Unwrapped) return;
        else await ParentViewModel.HandleTapAsync();
    }

    public override void HandleProfileTap() => ParentViewModel.BaseViewModel.RequestNavigation(typeof(ProfilePage), Comment.User.UserId);
}
