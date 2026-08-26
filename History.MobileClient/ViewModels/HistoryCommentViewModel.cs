using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using History.Commons.Api.Comment;
using History.Commons.Api.Moderation;
using History.Commons.Api.Report;
using History.Commons.DataTypes.Contents;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using History.MobileClient.Messages;
using History.MobileClient.Pages;

namespace History.MobileClient.ViewModels;

public partial class HistoryCommentViewModel : BaseCommentViewModel
{
    [ObservableProperty]
    public partial CommentResponseDto Comment { get; private set; }

    // Comment-dependent properties — set in UpdateComment.
    [ObservableProperty]
    public partial bool IsMyComment { get; private set; }
    [ObservableProperty]
    public partial bool Liked { get; private set; }

    public HistoryCommentViewModel(CommentResponseDto comment, bool isMyPost, PostType postType, BasePostViewModel parentViewModel) : base(isMyPost, postType, parentViewModel)
    {
        UpdateComment(comment);
        WeakReferenceMessenger.Default.Register<ValueChangedMessage<CommentResponseDto>>(this, (r, m) =>
        {
            if (m.Value.Id != Comment.Id) return;

            UpdateComment(m.Value);
        });
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
            ProfileMedia = user.UsesAnimatedProfileMedia
                ? new ImageViewModel(Utils.GenerateMediaUri(user.ProfileMediaId) ?? Constants.DefaultProfileImageFileName) { IsAnimated = true }
                : new ImageViewModel(Utils.GenerateMediaUri(user.ProfileMediaId) ?? Constants.DefaultProfileImageFileName);

            IsMyComment = user.UserId == Shared.UserId;
            HasLikes = comment.LikedUsers.Count > 0;
            LikesCount = comment.LikedUsers.Count;
            Liked = comment.LikedUsers.Any(x => x.UserId == Shared.UserId);

            var contents = Utils.GenerateContentViewModels(comment.Contents, PostType);
            var imageViewModels = (contents.FirstOrDefault(x => x is BaseWrappedMediaContentsViewModel) as BaseWrappedMediaContentsViewModel)?.Medias.Select(x => x.ImageMedia);
            imageViewModels ??= contents.OfType<BaseMediaContentViewModel>().Select(x => x.ImageMedia);
            foreach (var imageViewModel in imageViewModels.Cast<ImageViewModel>()) imageViewModel.MaxWidth = 200;

            Contents = contents;

            CreatedAt = comment.CreatedAt;
            ModifiedAt = comment.ModifiedAt;
            TimestampText = Utils.GenerateFriendlyTimestamp(CreatedAt, ModifiedAt);

            // Assign Comment last so all derived properties are already up-to-date.
            Comment = comment;
        }
        catch (ObjectDisposedException) { } // The view is disposed. this view model also will be removed on next GC
        catch (Exception) { } // Ignore any exceptions during update, as the view might be in the foreground.
    }

    public override List<BaseContent> GetRenderRawContents() => Comment.Contents;

    public override async Task HandleMore()
    {
        var actions = new List<string>
        {
            Liked ? "좋아요 취소" : "좋아요",
            IsMyComment ? "댓글 수정" : null,
            IsMyComment || IsMyPost || Shared.MyRank >= Rank.Moderator ? "댓글 삭제" : null,
            IsMyComment || Shared.MyRank >= Rank.Moderator ? null : "댓글 신고",
        };
        actions.RemoveAll(x => x == null);

        var action = await App.Page.DisplayActionSheetAsync("댓글 관리", Constants.PromptCancel, null, [.. actions]);
        if (action == null || action == Constants.PromptCancel) return;

        if (action.StartsWith("좋아요")) await HandleLikeAsync();
        else if (action == "댓글 수정") await App.PushAsync(new EditCommentPage(Comment));
        else if (action == "댓글 삭제") await DeleteAsync();
        else if (action == "댓글 신고")
        {
            var reportTypes = Enum.GetValues<ReportType>().Select(x => x.ToDisplayString()).ToArray();

            var rawReportType = await App.Page.DisplayActionSheetAsync("신고 카테고리", Constants.PromptCancel, null, reportTypes);
            if (rawReportType == null || rawReportType == Constants.PromptCancel) return;
            var reportType = ReportTypeExtensions.FromDisplayString(rawReportType);

            var result = await App.ExecuteRequestAsync(new CreateReportRecord(new()
            {
                Type = reportType,
                Target = ReportTarget.Comment,
                AssociatedId = Comment.Id
            }));

            if (result.IsSuccess) await App.Page.DisplayAlertAsync("안내", "댓글 신고가 성공적으로 전송되었습니다. 관리자 검토 후 처리 예정입니다.", Constants.PromptOk);
        }
        else await App.Page.DisplayAlertAsync("안내", "아직 지원하지 않는 기능입니다.", Constants.PromptOk);
    }

    public override async Task HandleCommentLikeTapAsync()
    {
        var users = Comment.LikedUsers;
        if (users.Count == 0)
        {
            await App.Page.DisplayAlertAsync("오류", "이 댓글에 좋아요를 누른 사용자가 없습니다.", Constants.PromptOk);
            return;
        }

        var viewModels = users.Select(x => new HistoryFriendshipViewModel(x, new HistoryInteractionViewModel(x)));
        var page = new InteractionsPage(viewModels, InteractionType.CommentLike);
        await App.PushAsync(page);
    }

    public override async Task HandleLikeAsync()
    {
        var commentResult = await App.ExecuteRequestAsync(new HandleCommentLike(Comment.Id));
        if (commentResult.IsSuccess) WeakReferenceMessenger.Default.Send(new ValueChangedMessage<CommentResponseDto>(commentResult.Value));
    }

    public override async Task DeleteAsync()
    {
        if (Comment.User.UserId != Shared.UserId && Shared.MyRank >= Rank.Moderator)
        {
            var reportTypes = Enum.GetValues<ReportType>().Select(x => x.ToDisplayString()).ToArray();
            var action = await App.Page.DisplayActionSheetAsync("제재 카테고리 선택", Constants.PromptCancel, null, reportTypes);
            if (action == null || action == Constants.PromptCancel) return;
            var reportType = ReportTypeExtensions.FromDisplayString(action);

            var reason = await App.Page.DisplayPromptAsync("댓글 삭제", "댓글을 삭제하는 이유를 입력해주세요.", "삭제", "취소", "삭제 사유");
            if (string.IsNullOrWhiteSpace(reason)) return;

            var deleteResult = await App.ExecuteRequestAsync(new ModerationDeleteComment(Comment.Id, reason, reportType));
            if (deleteResult.IsSuccess) WeakReferenceMessenger.Default.Send(new ValueDeletedMessage<CommentResponseDto>(Comment));
        }
        else if (IsMyPost || Comment.User.UserId == Shared.UserId)
        {
            var result = await App.Page.DisplayAlertAsync("댓글 삭제", "정말로 댓글을 삭제하시겠습니까?", Constants.PromptOk, Constants.PromptCancel);
            if (!result) return;

            var deleteResult = await App.ExecuteRequestAsync(new DeleteComment(Comment.Id));
            if (deleteResult.IsSuccess) WeakReferenceMessenger.Default.Send(new ValueDeletedMessage<CommentResponseDto>(Comment));
        }
        else await App.Page.DisplayAlertAsync("권한 부족", "댓글을 삭제할 권한이 없습니다.", Constants.PromptOk);
    }

    public override async Task HandleTapAsync()
    {
        if (PostType == PostType.Unwrapped)
        {
            // As TouchGestureCompleted is set to Label, LongPress will also raise Tap event which we doesn't count as Tap event.
            if (!IsLongPressed) WeakReferenceMessenger.Default.Send<CommentTappedMessage>(new(Comment.User));
            else IsLongPressed = false; // Reset the flag and never raise the event
        }
        else await ParentViewModel.HandleTapAsync();
    }

    public override async Task HandleProfileTap()
    {
        var userPage = new BlazorUserPage(Comment.User.UserId);
        await App.PushAsync(userPage);
    }
}
