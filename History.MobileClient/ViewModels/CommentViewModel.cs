using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using History.Commons.Api.Comment;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using History.MobileClient.DataTypes;
using History.MobileClient.Pages;

namespace History.MobileClient.ViewModels;

public partial class CommentViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Nickname))]
    [NotifyPropertyChangedFor(nameof(IsModerator))]
    [NotifyPropertyChangedFor(nameof(IsAdmin))]
    [NotifyPropertyChangedFor(nameof(ProfileMedia))]
    [NotifyPropertyChangedFor(nameof(Contents))]
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

    public List<IContentViewModel> Contents => Utils.GenerateContentViewModels(Comment.Contents, false);

    public bool IsMyComment => Comment.User.UserId == Shared.UserId;

    public bool HasLikes => Comment.LikedUsers.Count > 0;
    public int LikesCount => Comment.LikedUsers.Count;
    public List<ProfileViewModel> LikedUsers => Comment.LikedUsers.Select(u => new ProfileViewModel(u)).ToList();

    public bool Liked => Comment.LikedUsers.Any(x => x.UserId == Shared.UserId);
    public string CommentLikeFontFamily => Liked ? "FASolid" : "FARegular";

    private readonly bool _isMyPost;

    public CommentViewModel(CommentResponseDto comment, bool isMyPost)
    {
        _isMyPost = isMyPost;
        Comment = comment;
        WeakReferenceMessenger.Default.Register<ValueChangedMessage<CommentResponseDto>>(this, (r, m) =>
        {
            if (m.Value.Id != Comment.Id) return;

            Comment = m.Value;
        });
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
            IsMyComment || Shared.MyRank < Rank.Moderator ? null : "댓글 신고",
        };
        actions.RemoveAll(x => x == null);

        var action = await App.Page.DisplayActionSheet("댓글 관리", Constants.PromptCancel, null, [.. actions]);
        if (action == null || action == Constants.PromptCancel) return;

        if (action.StartsWith("좋아요")) await HandleLikeAsync();
        else if (action == "댓글 수정") await App.PushModalAsync(new EditCommentPage(Comment));
        else if (action == "댓글 삭제") await DeleteAsync();
        //else if (action == "댓글 신고")
        //{
            //var result = await App.ExecuteRequestAsync(new ReportComment(Comment.Id));
            //if (result.IsSuccess) await App.Page.DisplayAlert("신고 완료", "댓글이 신고되었습니다.", Constants.PromptOk);
        //}
        else await App.Page.DisplayAlert("안내", "아직 지원하지 않는 기능입니다.", Constants.PromptOk);
    }

    public async Task HandleLikeAsync()
    {
        var commentResult = await App.ExecuteRequestAsync(new HandleCommentLike(Comment.Id));
        if (commentResult.IsSuccess) WeakReferenceMessenger.Default.Send(new ValueChangedMessage<CommentResponseDto>(commentResult.Value));
    }

    public async Task DeleteAsync()
    {
        var result = await App.Page.DisplayAlert("댓글 삭제", "정말로 댓글을 삭제하시겠습니까?", Constants.PromptOk, Constants.PromptCancel);
        if (!result) return;

        var commentResult = await App.ExecuteRequestAsync(new DeleteComment(Comment.Id));
        if (commentResult.IsSuccess) WeakReferenceMessenger.Default.Send(new ValueDeletedMessage<CommentResponseDto>(Comment));
    }

    [RelayCommand]
    public void HandleTap() => WeakReferenceMessenger.Default.Send<CommentTappedMessage>(new(Comment.User));

    [RelayCommand]
    public async Task HandleProfileTap()
    {
        var userPage = new UserPage(Comment.User.UserId);
        await App.PushModalAsync(userPage);
    }
}
