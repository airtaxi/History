using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using History.Commons;
using History.MobileClient.DataTypes;
using History.MobileClient.Enums;
using History.MobileClient.KakaoStory;
using History.MobileClient.Pages;
using static History.MobileClient.KakaoStory.KakaoStoryApiHandler.DataType;
using static History.MobileClient.KakaoStory.KakaoStoryApiHandler.DataType.CommentData;

namespace History.MobileClient.ViewModels;

// Kakao Story comment view model: fills the shared comment surface from the feed-embedded
// comment (timeline preview) or the full Comment (API responses) — both are the same shape.
// Like/delete work from the timeline; edit/detail/profile pages are TODO stubs.
public partial class KakaoCommentViewModel : BaseCommentViewModel
{
    [ObservableProperty]
    public partial Comment Comment { get; private set; }

    [ObservableProperty]
    public partial bool IsMyComment { get; private set; }

    [ObservableProperty]
    public partial bool Liked { get; private set; }

    private readonly KakaoPostViewModel _parentPostViewModel;
    private string _commentId;
    private string _commentUserId;
    private string _commentNickname;
    private List<QuoteData> _decorators;
    private int _updateVersion;

    private string PostId => Comment?.activity_id ?? _parentPostViewModel.PostData.id;

    public KakaoCommentViewModel(Comment comment, PostType postType, KakaoPostViewModel parentViewModel) : base(parentViewModel.PostData.actor?.id == Shared.KakaoUserId, postType, parentViewModel)
    {
        _parentPostViewModel = parentViewModel;
        UpdateComment(comment);
        WeakReferenceMessenger.Default.Register<ValueChangedMessage<Comment>>(this, (r, m) =>
        {
            if (m.Value.id != _commentId) return;

            UpdateComment(m.Value);
        });
    }

    private void UpdateComment(Comment comment)
    {
        var version = ++_updateVersion;
        try
        {
            var writer = comment.writer;
            _commentId = comment.id;
            _commentUserId = writer?.id;
            _commentNickname = writer?.display_name;
            _decorators = comment.decorators;

            Nickname = writer?.display_name;
            IsModerator = false;
            IsAdmin = false;
            ProfileMedia = writer?.profile_image_url != null ? new ImageViewModel(writer.profile_image_url) : null;

            IsMyComment = writer?.id == Shared.KakaoUserId;
            HasLikes = comment.like_count > 0;
            LikesCount = comment.like_count;
            Liked = comment.liked;

            var contents = new List<IContentViewModel>
            {
                new TextTypeContentsViewModel(comment.decorators is { Count: > 0 } ? comment.decorators : KakaoStoryUtils.GetQuoteDataFromString(comment.text ?? string.Empty), PostType)
            };

            // Render the comment image (embedded as a media decorator) with the shared media surface.
            var commentMedia = comment.decorators?.FirstOrDefault(x => x.media?.thumbnail_url != null)?.media;
            if (commentMedia != null)
            {
                var medium = new Medium
                {
                    media_path = commentMedia.media_path,
                    thumbnail_url = commentMedia.thumbnail_url,
                    url = commentMedia.url,
                    origin_url = commentMedia.origin_url,
                    content_type = "image",
                    width = commentMedia.width,
                    height = commentMedia.height
                };
                var mediaViewModel = new KakaoMediaContentViewModel(medium, [medium], PostType);
                if (mediaViewModel.ImageMedia is ImageViewModel commentImage)
                {
                    commentImage.MaxWidth = 200;
                    commentImage.HorizontalContentOptions = LayoutOptions.Start;
                    commentImage.VerticalContentOptions = LayoutOptions.Start;
                    commentImage.Aspect = Aspect.AspectFit;
                }
                contents.Add(mediaViewModel);
            }
            Contents = contents;

            CreatedAt = comment.created_at;
            ModifiedAt = comment.updated_at.Year > 1 ? comment.updated_at : null;
            TimestampText = KakaoStoryUtils.GetTimeString(CreatedAt);

            Comment = comment;
        }
        catch (ObjectDisposedException) { }
        catch (Exception) { }

        // Fire-and-forget: render emoticons (Referer-signed) once the credential
        // is available; until then the "(이모티콘)" text placeholder is shown.
        _ = AttachEmoticonsAsync(version);
    }

    private async Task AttachEmoticonsAsync(int version)
    {
        try
        {
            var emoticonContents = await KakaoStoryUtils.BuildEmoticonContentsAsync(_decorators, PostType);
            if (emoticonContents == null) return;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (version != _updateVersion) return; // Stale update.
                var contents = emoticonContents.ToList();

                // Append the comment image (embedded as a media decorator) after
                // the text/emoticon fragments, mirroring UpdateComment's layout.
                var commentMedia = _decorators?.FirstOrDefault(x => x.media?.thumbnail_url != null)?.media;
                if (commentMedia != null)
                {
                    var medium = new Medium
                    {
                        media_path = commentMedia.media_path,
                        thumbnail_url = commentMedia.thumbnail_url,
                        url = commentMedia.url,
                        origin_url = commentMedia.origin_url,
                        content_type = "image",
                        width = commentMedia.width,
                        height = commentMedia.height
                    };
                    var mediaViewModel = new KakaoMediaContentViewModel(medium, [medium], PostType);
                    if (mediaViewModel.ImageMedia is ImageViewModel commentImage)
                    {
                        commentImage.MaxWidth = 200;
                        commentImage.HorizontalContentOptions = LayoutOptions.Start;
                        commentImage.VerticalContentOptions = LayoutOptions.Start;
                        commentImage.Aspect = Aspect.AspectFit;
                    }
                    contents.Add(mediaViewModel);
                }

                Contents = contents;
            });
        }
        catch { } // Credential/URL failures keep the text placeholder.
    }

    public override async Task HandleLikeAsync()
    {
        var commentResult = await KakaoStoryApiHandler.LikeComment(PostId, _commentId, Liked);
        if (commentResult == null) return;

        // Like responses may omit decorators — preserve the current ones (KSMP pattern).
        if (commentResult.decorators is not { Count: > 0 }) commentResult.decorators = _decorators;

        WeakReferenceMessenger.Default.Send(new ValueChangedMessage<Comment>(commentResult));
    }

    public override async Task HandleCommentLikeTapAsync()
    {
        var likes = await KakaoStoryApiHandler.GetCommentLikes(PostId, _commentId);
        if (likes == null || likes.Count == 0)
        {
            await App.Page.DisplayAlertAsync("안내", "이 댓글에 좋아요를 누른 사용자가 없습니다.", Constants.PromptOk);
            return;
        }

        var viewModels = likes.Select(x => new KakaoFriendshipViewModel(x));
        var page = new InteractionsPage(viewModels, InteractionType.CommentLike);
        await App.PushAsync(page);
    }

    public override async Task HandleMore()
    {
        var actions = new List<string>
        {
            Liked ? "좋아요 취소" : "좋아요",
            IsMyComment ? "댓글 수정" : null,
            IsMyComment || IsMyPost ? "댓글 삭제" : null,
        };
        actions.RemoveAll(x => x == null);

        var action = await App.Page.DisplayActionSheetAsync("댓글 관리", Constants.PromptCancel, null, [.. actions]);
        if (action == null || action == Constants.PromptCancel) return;

        if (action.StartsWith("좋아요")) await HandleLikeAsync();
        else if (action == "댓글 수정") await HandleEditCommentAsync();
        else if (action == "댓글 삭제") await DeleteAsync();
    }

    public override async Task DeleteAsync()
    {
        var confirm = await App.Page.DisplayAlertAsync("댓글 삭제", "정말로 댓글을 삭제하시겠습니까?", Constants.PromptOk, Constants.PromptCancel);
        if (!confirm) return;

        try
        {
            await KakaoStoryApiHandler.DeleteComment(_commentId, PostId);
            await _parentPostViewModel.RefreshAsync();
        }
        catch (Exception exception) { await App.Page.DisplayAlertAsync("오류", $"댓글 삭제에 실패하였습니다.\n{exception.Message}", Constants.PromptOk); }
    }

    public override async Task HandleTapAsync()
    {
        if (PostType == PostType.Unwrapped)
        {
            // As TouchGestureCompleted is set to Label, LongPress will also raise Tap event which we doesn't count as Tap event.
            if (!IsLongPressed) WeakReferenceMessenger.Default.Send<CommentTappedMessage>(new(_commentUserId, _commentNickname));
            else IsLongPressed = false; // Reset the flag and never raise the event
        }
        else await ParentViewModel.HandleTapAsync();
    }

    // TODO: Kakao Story profile page — pending page migration.
    public override async Task HandleProfileTap() => await App.Page.DisplayAlertAsync("안내", "카카오스토리 프로필 페이지는 아직 지원되지 않습니다.", Constants.PromptOk);

    // TODO: Comment editing requires the Kakao Story comment editor page — pending page migration.
    private async Task HandleEditCommentAsync() => await App.Page.DisplayAlertAsync("안내", "카카오스토리 댓글 수정은 아직 지원되지 않습니다.", Constants.PromptOk);
}
