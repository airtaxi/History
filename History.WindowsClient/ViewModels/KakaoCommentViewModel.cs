using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using History.Commons;
using History.Commons.DataTypes.Contents;
using History.Commons.Enums;
using History.Commons.KakaoStory;
using History.WindowsClient.Helpers;
using History.WindowsClient.Messages;
using History.WindowsClient.Models;
using History.WindowsClient.Pages;
using History.WindowsClient.Views;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using static History.Commons.KakaoStory.KakaoStoryApiHandler.DataType;
using static History.Commons.KakaoStory.KakaoStoryApiHandler.DataType.CommentData;

namespace History.WindowsClient.ViewModels;

// Kakao Story comment view model: fills the shared comment surface from a feed comment.
// Like, edit, and delete are available from the comment menu.
public partial class KakaoCommentViewModel : BaseCommentViewModel, IRecipient<ValueChangedMessage<Comment>>
{
    private readonly BaseViewModel _baseViewModel;
    private readonly KakaoPostViewModel _parentPostViewModel;
    private string _commentId;
    private List<QuoteData> _decorators;

    [ObservableProperty]
    public partial Comment Comment { get; private set; }

    private string PostId => Comment?.activity_id ?? _parentPostViewModel.PostData.id;

    public KakaoCommentViewModel(Comment comment, PostType postType, KakaoPostViewModel parentViewModel) : base(parentViewModel.PostData.actor?.id == CommonShared.KakaoUserId, postType, parentViewModel)
    {
        _baseViewModel = parentViewModel.BaseViewModel;
        _parentPostViewModel = parentViewModel;
        UpdateComment(comment);

        WeakReferenceMessenger.Default.Register(this);
    }

    public void Receive(ValueChangedMessage<Comment> message)
    {
        if (message.Value.id != _commentId) return;

        UpdateComment(message.Value);
    }

    private void UpdateComment(Comment comment)
    {
        // Compute all derived properties from the new comment before assigning Comment.
        var writer = comment.writer;
        _commentId = comment.id;
        _decorators = comment.decorators is { Count: > 0 } ? comment.decorators : KakaoStoryUtils.GetQuoteDataFromString(comment.text ?? string.Empty);

        Nickname = writer?.display_name;
        IsModerator = false;
        IsAdmin = false;
        ProfileThumbnailImageSource = writer?.profile_image_url != null ? new BitmapImage(new Uri(writer.profile_image_url)) : null;

        IsMyComment = writer?.id == CommonShared.KakaoUserId;
        HasLikes = comment.like_count > 0;
        LikesCount = comment.like_count;
        Liked = comment.liked;
        // The like user list loads on demand when its flyout opens.
        LikedUsers = [];

        Contents = PostHelper.GenerateContentViewModels(KakaoStoryUtils.ConvertToBaseContents(_decorators, true), PostType, _baseViewModel);

        CreatedAt = comment.created_at;
        ModifiedAt = comment.updated_at.Year > 1 ? comment.updated_at : null;
        TimestampText = KakaoStoryUtils.GetTimeString(CreatedAt, ModifiedAt);

        Comment = comment;
    }

    public override string ProfileMediaUri => Comment?.writer?.profile_image_url;

    public override List<BaseContent> GetRenderRawContents() => KakaoStoryUtils.ConvertToBaseContents(_decorators, true);

    public override void PopulateMoreMenuFlyout(MenuFlyout menuFlyout)
    {
        menuFlyout.Items.Clear();

        if (Liked) menuFlyout.Items.Add(Utils.CreateActionItem("좋아요 취소", "\uEA92", HandleLikeAsync));
        else menuFlyout.Items.Add(Utils.CreateActionItem("좋아요", "\uEB52", HandleLikeAsync));

        if (IsMyComment) menuFlyout.Items.Add(Utils.CreateActionItem("댓글 수정", "\uE70F", HandleEditComment));
        if (IsMyComment || IsMyPost) menuFlyout.Items.Add(Utils.CreateActionItem("댓글 삭제", "\uE74D", DeleteAsync));
    }

    // Opens the comment edit window as a modal of the main window; the window closes itself
    // after a successful edit.
    private void HandleEditComment() => new EditCommentWindow(new EditCommentWindowViewModel(Comment, PostId)).MakeModal(MainWindow.Instance);

    public override Task HandleMore() => Task.CompletedTask;

    // Feed comments carry only the like count, so the like user list is fetched on demand
    // when its flyout opens.
    public override async Task LoadLikedUsersAsync()
    {
        if (LikedUsers.Count > 0) return;

        var likes = await _baseViewModel.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.GetCommentLikes(PostId, _commentId));
        LikedUsers = likes?.Select(x => (BaseFriendshipViewModel)new KakaoFriendshipViewModel(x, _baseViewModel)).ToList() ?? [];
    }

    public override async Task HandleLikeAsync()
    {
        var commentResult = await _baseViewModel.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.LikeComment(PostId, _commentId, Liked));
        if (commentResult == null) return;

        // Like responses may omit the decorators; keep the current ones.
        if (commentResult.decorators is not { Count: > 0 }) commentResult.decorators = _decorators;
        WeakReferenceMessenger.Default.Send(new ValueChangedMessage<Comment>(commentResult));
    }

    public override async Task DeleteAsync()
    {
        var confirm = await _baseViewModel.ShowMessageDialogAsync(new("댓글 삭제", "정말로 댓글을 삭제하시겠습니까?", DialogHelper.DefaultOkButtonText, DialogHelper.DefaultCancelButtonText));
        if (confirm != ContentDialogResult.Primary) return;

        try
        {
            await _baseViewModel.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.DeleteComment(_commentId, PostId));
            await _parentPostViewModel.RefreshAsync();
        }
        catch (Exception exception) { await _baseViewModel.ShowMessageDialogAsync(new(Constants.ErrorTitle, $"댓글 삭제에 실패하였습니다.\n{exception.Message}")); }
    }

    public override async Task HandleTapAsync()
    {
        // The comment editor listens for comment taps in the unwrapped post view.
        if (PostType == PostType.Unwrapped) return;
        else await ParentViewModel.HandleTapAsync();
    }

    public override void HandleProfileTap()
    {
        if (Comment?.writer?.id == null) return;

        _baseViewModel.RequestNavigation(typeof(ProfilePage), new KakaoProfileParameters(Comment.writer.id));
    }

    // Requests the post page to insert a mention of the comment author into the comment editor.
    public override void HandleReply()
    {
        if (PostType == PostType.Unwrapped && !IsMyComment && Comment?.writer?.id != null)
        {
            WeakReferenceMessenger.Default.Send(new CommentReplyRequestedMessage(new ProfileContent { UserId = Comment.writer.id, Nickname = Nickname }));
        }
    }
}
