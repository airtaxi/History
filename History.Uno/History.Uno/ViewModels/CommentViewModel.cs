using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using History.Commons.Api.Comment;
using History.Commons.Api.Moderation;
using History.Commons.Api.Report;
using History.Commons.DataTypes.ResponseDtos;
using History.Uno.DataTypes;
using History.Uno.Enums;
using Microsoft.UI.Xaml.Controls;

namespace History.Uno.ViewModels;

public partial class CommentViewModel : ObservableObject
{
    [ObservableProperty]
    public partial CommentResponseDto Comment { get; private set; }

    // User-dependent properties — set in UpdateComment alongside Comment assignment.
    [ObservableProperty]
    public partial string Nickname { get; private set; }
    [ObservableProperty]
    public partial bool IsModerator { get; private set; }
    [ObservableProperty]
    public partial bool IsAdmin { get; private set; }
    [ObservableProperty]
    public partial IMediaViewModel ProfileMedia { get; private set; }

    // Comment-dependent properties — set in UpdateComment.
    [ObservableProperty]
    public partial bool IsMyComment { get; private set; }
    [ObservableProperty]
    public partial bool HasLikes { get; private set; }
    [ObservableProperty]
    public partial int LikesCount { get; private set; }
    [ObservableProperty]
    public partial bool Liked { get; private set; }

    [ObservableProperty]
    public partial List<IContentViewModel> Contents { get; private set; }

    [ObservableProperty]
    public partial DateTime CreatedAt { get; private set; }
    [ObservableProperty]
    public partial DateTime? ModifiedAt { get; private set; }
    [ObservableProperty]
    public partial string TimestampText { get; private set; }

    public bool IsLongPressed { get; set; }

    private readonly bool _isMyPost;
    private readonly PostType _postType;
    private readonly PostViewModel _parentViewModel;

    public CommentViewModel(CommentResponseDto comment, bool isMyPost, PostType postType, PostViewModel parentViewModel)
    {
        _isMyPost = isMyPost;
        _postType = postType;
        _parentViewModel = parentViewModel;

        UpdateComment(comment);
        WeakReferenceMessenger.Default.Register<ValueChangedMessage<CommentResponseDto>>(this, (recipient, message) =>
        {
            if (message.Value.Id != Comment.Id) return;

            UpdateComment(message.Value);
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
            ProfileMedia = new ImageViewModel(Utils.GenerateMediaUri(user.ProfileMediaId) ?? Constants.DefaultProfileImageFileName);

            IsMyComment = user.UserId == Shared.UserId;
            HasLikes = comment.LikedUsers.Count > 0;
            LikesCount = comment.LikedUsers.Count;
            Liked = comment.LikedUsers.Any(x => x.UserId == Shared.UserId);

            var contents = Utils.GenerateContentViewModels(comment.Contents, _postType);
            var imageViewModels = (contents.FirstOrDefault(x => x is WrappedMediaContentsViewModel) as WrappedMediaContentsViewModel)?.Medias.Select(x => x.ImageMedia);
            imageViewModels ??= contents.OfType<MediaContentViewModel>().Select(x => x.ImageMedia);
            foreach (ImageViewModel imageViewModel in imageViewModels) imageViewModel.MaxWidth = 200;

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

    [RelayCommand]
    public async Task HandleMore()
    {
        var actions = new List<string>
        {
            Liked ? "좋아요 취소" : "좋아요",
            IsMyComment ? "댓글 수정" : null,
            IsMyComment || _isMyPost || Shared.MyRank >= Rank.Moderator ? "댓글 삭제" : null,
            IsMyComment || Shared.MyRank >= Rank.Moderator ? null : "댓글 신고",
        };
        actions.RemoveAll(x => x == null);

        var action = await App.DisplayActionSheetAsync("댓글 관리", Constants.PromptCancel, null, [.. actions]);
        if (action == null || action == Constants.PromptCancel) return;

        if (action.StartsWith("좋아요")) await HandleLikeAsync();
        else if (action == "댓글 수정")
        {
            // TODO: Navigate to EditCommentPage (migrated in a later phase).
            await App.DisplayAlertAsync("안내", "댓글 수정은 아직 지원되지 않습니다.", Constants.PromptOk);
        }
        else if (action == "댓글 삭제") await DeleteAsync();
        else if (action == "댓글 신고")
        {
            var reportTypes = Enum.GetValues<ReportType>().Select(x => x.ToDisplayString()).ToArray();

            var rawReportType = await App.DisplayActionSheetAsync("신고 카테고리", Constants.PromptCancel, null, reportTypes);
            if (rawReportType == null || rawReportType == Constants.PromptCancel) return;
            var reportType = ReportTypeExtensions.FromDisplayString(rawReportType);

            var result = await App.ExecuteRequestAsync(new CreateReportRecord(new()
            {
                Type = reportType,
                Target = ReportTarget.Comment,
                AssociatedId = Comment.Id
            }));

            if (result.IsSuccess) await App.DisplayAlertAsync("안내", "댓글 신고가 성공적으로 전송되었습니다. 관리자 검토 후 처리 예정입니다.", Constants.PromptOk);
        }
        else await App.DisplayAlertAsync("안내", "아직 지원하지 않는 기능입니다.", Constants.PromptOk);
    }

    [RelayCommand]
    public async Task HandleCommentLikeTapAsync()
    {
        if (Comment.LikedUsers.Count == 0)
        {
            await App.DisplayAlertAsync("오류", "이 댓글에 좋아요를 누른 사용자가 없습니다.", Constants.PromptOk);
            return;
        }

        // TODO: Navigate to InteractionsPage (migrated in a later phase).
        await App.DisplayAlertAsync("안내", "좋아요 목록 페이지는 아직 지원되지 않습니다.", Constants.PromptOk);
    }

    public async Task HandleLikeAsync()
    {
        var commentResult = await App.ExecuteRequestAsync(new HandleCommentLike(Comment.Id));
        if (commentResult.IsSuccess) WeakReferenceMessenger.Default.Send(new ValueChangedMessage<CommentResponseDto>(commentResult.Value));
    }

    public async Task DeleteAsync()
    {
        if (Comment.User.UserId != Shared.UserId && Shared.MyRank >= Rank.Moderator)
        {
            var reportTypes = Enum.GetValues<ReportType>().Select(x => x.ToDisplayString()).ToArray();
            var action = await App.DisplayActionSheetAsync("제재 카테고리 선택", Constants.PromptCancel, null, reportTypes);
            if (action == null || action == Constants.PromptCancel) return;
            var reportType = ReportTypeExtensions.FromDisplayString(action);

            var reason = await App.DisplayPromptAsync("댓글 삭제", "댓글을 삭제하는 이유를 입력해주세요.", "삭제", "취소", "삭제 사유");
            if (string.IsNullOrWhiteSpace(reason)) return;

            var deleteResult = await App.ExecuteRequestAsync(new ModerationDeleteComment(Comment.Id, reason, reportType));
            if (deleteResult.IsSuccess) WeakReferenceMessenger.Default.Send(new ValueDeletedMessage<CommentResponseDto>(Comment));
        }
        else if (_isMyPost || Comment.User.UserId == Shared.UserId)
        {
            var result = await App.DisplayAlertAsync("댓글 삭제", "정말로 댓글을 삭제하시겠습니까?", Constants.PromptOk, Constants.PromptCancel);
            if (result != ContentDialogResult.Primary) return;

            var deleteResult = await App.ExecuteRequestAsync(new DeleteComment(Comment.Id));
            if (deleteResult.IsSuccess) WeakReferenceMessenger.Default.Send(new ValueDeletedMessage<CommentResponseDto>(Comment));
        }
        else await App.DisplayAlertAsync("권한 부족", "댓글을 삭제할 권한이 없습니다.", Constants.PromptOk);
    }

    [RelayCommand]
    public async Task HandleTapAsync()
    {
        if (_postType == PostType.Unwrapped)
        {
            // As long press handling is not wired in Uno yet, every tap is treated as a tap event.
            WeakReferenceMessenger.Default.Send<CommentTappedMessage>(new(Comment.User));
        }
        else await _parentViewModel.HandleTapAsync();
    }

    [RelayCommand]
    public async Task HandleProfileTap() => await App.PushAsync(typeof(Pages.UserPage), Comment.User.UserId);
}
