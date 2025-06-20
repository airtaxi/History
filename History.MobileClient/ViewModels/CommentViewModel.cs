using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using History.Commons.Api.Comment;
using History.Commons.Api.Moderation;
using History.Commons.Api.Report;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using History.MobileClient.DataTypes;
using History.MobileClient.Enums;
using History.MobileClient.Pages;

namespace History.MobileClient.ViewModels;

public partial class CommentViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Nickname))]
    [NotifyPropertyChangedFor(nameof(IsModerator))]
    [NotifyPropertyChangedFor(nameof(IsAdmin))]
    [NotifyPropertyChangedFor(nameof(ProfileMedia))]
    [NotifyPropertyChangedFor(nameof(IsMyComment))]
    [NotifyPropertyChangedFor(nameof(HasLikes))]
    [NotifyPropertyChangedFor(nameof(LikesCount))]
    [NotifyPropertyChangedFor(nameof(LikedUsers))]
    [NotifyPropertyChangedFor(nameof(Liked))]
    [NotifyPropertyChangedFor(nameof(CommentLikeFontFamily))]
    [NotifyPropertyChangedFor(nameof(CommentLikeColor))]
    [NotifyPropertyChangedFor(nameof(CreatedAt))]
    [NotifyPropertyChangedFor(nameof(ModifiedAt))]
    [NotifyPropertyChangedFor(nameof(TimestampText))]
    public partial CommentResponseDto Comment { get; set; }

    public string Nickname => Comment.User.Nickname;
    public bool IsModerator => Comment.User.Rank == Rank.Moderator;
    public bool IsAdmin => Comment.User.Rank == Rank.Admin;
    public IMediaViewModel ProfileMedia => Comment.User.UsesAnimatedProfileMedia
        ? new VideoViewModel(Utils.GenerateMediaUri(Comment.User.ProfileMediaId))
        : new ImageViewModel(Utils.GenerateMediaUri(Comment.User.ProfileMediaId) ?? Constants.DefaultProfileImageFileName);

    [ObservableProperty]
    public partial List<IContentViewModel> Contents { get; private set; }

    public bool IsMyComment => Comment.User.UserId == Shared.UserId;

    public bool HasLikes => Comment.LikedUsers.Count > 0;
    public int LikesCount => Comment.LikedUsers.Count;
    public List<ProfileViewModel> LikedUsers => Comment.LikedUsers.Select(u => new ProfileViewModel(u)).ToList();

    public bool Liked => Comment.LikedUsers.Any(x => x.UserId == Shared.UserId);
    public string CommentLikeFontFamily => Liked ? "FASolid" : "FARegular";

    private readonly bool _isMyPost;
    private readonly PostType _postType;
    private readonly PostViewModel _parentViewModel;

    public CommentViewModel(CommentResponseDto comment, bool isMyPost, PostType postType, PostViewModel parentViewModel)
    {
        _isMyPost = isMyPost;
        _postType = postType;
        _parentViewModel = parentViewModel;

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
            Comment = comment;
            Contents = Utils.GenerateContentViewModels(Comment.Contents, _postType);
        }
        catch (ObjectDisposedException) { } // The view is disposed. this view model also will be removed on next GC
        catch (Exception) { } // Ignore any exceptions during update, as the view might be in the foreground.
    }

    public Color CommentLikeColor => Liked ? Color.FromRgb(0xeb, 0x55, 0x27) : Color.FromRgb(0x80, 0x80, 0x80);

    public DateTime CreatedAt => Comment.CreatedAt;
    public DateTime? ModifiedAt => Comment.ModifiedAt;

    public string TimestampText => Utils.GenerateFriendlyTimestamp(CreatedAt, ModifiedAt);

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

        var action = await App.Page.DisplayActionSheet("댓글 관리", Constants.PromptCancel, null, [.. actions]);
        if (action == null || action == Constants.PromptCancel) return;

        if (action.StartsWith("좋아요")) await HandleLikeAsync();
        else if (action == "댓글 수정") await App.PushAsync(new EditCommentPage(Comment));
        else if (action == "댓글 삭제") await DeleteAsync();
        else if (action == "댓글 신고")
        {
            var reportTypes = Enum.GetValues<ReportType>().Select(x => x.ToDisplayString()).ToArray();

            var rawReportType = await App.Page.DisplayActionSheet("신고 카테고리", Constants.PromptCancel, null, reportTypes);
            if (rawReportType == null || rawReportType == Constants.PromptCancel) return;
            var reportType = ReportTypeExtensions.FromDisplayString(rawReportType);

            var result = await App.ExecuteRequestAsync(new CreateReportRecord(new()
            {
                Type = reportType,
                Target = ReportTarget.Comment,
                AssociatedId = Comment.Id
            }));

            if (result.IsSuccess) await App.Page.DisplayAlert("안내", "댓글 신고가 성공적으로 전송되었습니다. 관리자 검토 후 처리 예정입니다.", Constants.PromptOk);
        }
        else await App.Page.DisplayAlert("안내", "아직 지원하지 않는 기능입니다.", Constants.PromptOk);
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
            var action = await App.Page.DisplayActionSheet("제재 카테고리 선택", Constants.PromptCancel, null, reportTypes);
            if (action == null || action == Constants.PromptCancel) return;
            var reportType = ReportTypeExtensions.FromDisplayString(action);

            var reason = await App.Page.DisplayPromptAsync("댓글 삭제", "댓글을 삭제하는 이유를 입력해주세요.", "삭제", "취소", "삭제 사유");
            if (string.IsNullOrWhiteSpace(reason)) return;

            var deleteResult = await App.ExecuteRequestAsync(new ModerationDeleteComment(Comment.Id, reason, reportType));
            if (deleteResult.IsSuccess) WeakReferenceMessenger.Default.Send(new ValueDeletedMessage<CommentResponseDto>(Comment));
        }
        else if(_isMyPost || Comment.User.UserId == Shared.UserId)
        {
            var result = await App.Page.DisplayAlert("댓글 삭제", "정말로 댓글을 삭제하시겠습니까?", Constants.PromptOk, Constants.PromptCancel);
            if (!result) return;

            var deleteResult = await App.ExecuteRequestAsync(new DeleteComment(Comment.Id));
            if (deleteResult.IsSuccess) WeakReferenceMessenger.Default.Send(new ValueDeletedMessage<CommentResponseDto>(Comment));
        }
        else
        {
            await App.Page.DisplayAlert("권한 부족", "댓글을 삭제할 권한이 없습니다.", Constants.PromptOk);
            return;
        }
    }

    [RelayCommand]
    public async Task HandleTapAsync()
    {
        if (_postType == PostType.Unwrapped) WeakReferenceMessenger.Default.Send<CommentTappedMessage>(new(Comment.User));
        else await _parentViewModel.HandleTapAsync();
    }

    [RelayCommand]
    public async Task HandleProfileTap()
    {
        var userPage = new UserPage(Comment.User.UserId);
        await App.PushAsync(userPage);
    }
}
