using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using History.Commons;
using History.MobileClient.DataTypes;
using History.MobileClient.Enums;
using History.MobileClient.KakaoStory;
using History.MobileClient.Pages;
using UraniumUI.Icons.FontAwesome;
using static History.MobileClient.KakaoStory.KakaoStoryApiHandler.DataType;
using static History.MobileClient.KakaoStory.KakaoStoryApiHandler.DataType.CommentData;

namespace History.MobileClient.ViewModels;

public partial class KakaoPostViewModel : BasePostViewModel
{
    private PostData _postData;
    private int _updateVersion;

    public PostData PostData => _postData;
    public bool IsMyPost => _postData.actor?.id == Shared.KakaoUserId;

    protected PostData CurrentPostData
    {
        get => _postData;
        set => _postData = value;
    }

    public KakaoPostViewModel(PostData postData, PostType postType = PostType.Timeline, bool isParentPost = false) : base(postType, isParentPost)
    {
        RepostCountPrefix = "UP ";
        _postData = postData;
        UpdatePost(postData);

        WeakReferenceMessenger.Default.Register<ValueChangedMessage<PostData>>(this, OnPostChangedMessageReceived);
    }

    private void OnPostChangedMessageReceived(object _, ValueChangedMessage<PostData> message)
    {
        if (message.Value.id != _postData.id) return;

        UpdatePost(message.Value);
    }

    protected virtual void UpdatePost(PostData postData)
    {
        _postData = postData;
        var version = ++_updateVersion;
        try
        {
            var actor = postData.actor;

            Nickname = actor?.display_name;
            IsModerator = false;
            IsAdmin = false;
            ProfileMedia = (actor != null && actor.profile_image_url != null) ? new ImageViewModel(actor.profile_image_url) { IsAnimated = false } : null;

            Contents = GenerateContentViewModels(postData, PostType);
            TimelineContents = new TimelineContentsViewModel(Contents);
            // Kakao Story embeds the original post in @object for share/UP activities (KSMP pattern).
            // The embedded post renders as a nested card via SharedPostTemplate; tapping it opens the original post.
            ParentPost = postData.@object != null ? new KakaoPostViewModel(postData.@object, PostType, true) : null;
            IsRepost = false;
            IsShare = postData.@object != null;

            var sourceComments = postData.comments ?? postData.latest_comments ?? [];
            Comments = [.. sourceComments.Select(c => new KakaoCommentViewModel(c, PostType, this)).OrderBy(x => x.CreatedAt)];
            LatestComment = Comments.LastOrDefault();
            CommentsCount = postData.comment_count;
            HasComments = CommentsCount > 0;
            HasNoComments = CommentsCount == 0;
            HasMoreComments = CommentsCount > Comments.Count;

            CreatedAt = postData.created_at;
            // content_updated_at exists only when the content was actually edited
            // (updated_at also changes on comments/likes, so it is not reliable).
            ModifiedAt = postData.content_updated_at.Year > 1 ? postData.content_updated_at : null;
            TimestampText = KakaoStoryUtils.GetTimeString(postData.created_at, ModifiedAt);

            PreviewText = postData.summary ?? postData.content ?? string.Empty;
            PreviewTimestamp = postData.created_at.ToLocalTime().ToString("yyyy-MM-dd");
            PreviewThumbnailVisible = postData.media?.FirstOrDefault()?.thumbnail_url != null;
            HasUnreadNotification = postData.has_unread_reaction;
            PreviewThumbnail = PreviewThumbnailVisible ? new ImageViewModel(postData.media[0].thumbnail_url)
            {
                Aspect = Aspect.AspectFill,
                HorizontalContentOptions = LayoutOptions.Fill,
                VerticalContentOptions = LayoutOptions.Fill
            } : null;

            // Kakao Story permission mapping: M = Only me, F = Friends, A = Everyone.
            DiscoveryOptionGlyph = postData.permission switch
            {
                "M" => Solid.Lock,
                "F" => Solid.Users,
                "A" => Solid.Globe,
                _ => Solid.Question
            };
            var shareCount = Math.Max(0, postData.share_count - postData.sympathy_count); // Kakao's share_count includes sympathy (UP) actions.
            HasSharedUsers = shareCount > 0;
            SharedUsersCount = shareCount;
            HasReactions = postData.like_count > 0;
            ReactionsCount = postData.like_count;
            var reactionVisual = KakaoStoryUtils.GetEmotionVisual(_postData.liked_emotion);
            ReactionGlyph = reactionVisual.Glyph;
            ReactionFontFamily = postData.liked ? "FASolid" : "FARegular";
            ReactionColor = postData.liked ? reactionVisual.Color : (Utils.GetGlobalAppTheme() == AppTheme.Dark ? Colors.White : Colors.Black);
            HasRepostedUsers = postData.sympathy_count > 0;
            RepostedUsersCount = postData.sympathy_count;
            Interactions = postData.likes?.Select(x => (BaseInteractionViewModel)new KakaoInteractionViewModel(x)).ToList() ?? [];
            Reaction = null;
            HasInteractions = HasReactions || HasSharedUsers || HasRepostedUsers;
        }
        catch (ObjectDisposedException) { }
        catch (Exception) { }

        // Fire-and-forget: render emoticons (Referer-signed) once the credential
        // is available; until then the "(이모티콘)" text placeholder is shown.
        _ = AttachEmoticonsAsync(version);

        // Fire-and-forget: fetch share/UP user profile photos for the detail page interaction list.
        // The timeline template only shows counts, so we avoid extra API calls on the feed.
        // The embedded original post (ParentPost) never shows its own interaction list.
        if (PostType == PostType.Unwrapped && !IsParentPost) _ = AttachInteractionsAsync(version);
    }

    private async Task AttachEmoticonsAsync(int version)
    {
        try
        {
            var quoteDatas = _postData.content_decorators;
            var emoticonContents = await KakaoStoryUtils.BuildEmoticonContentsAsync(quoteDatas, PostType);
            if (emoticonContents == null) return;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (version != _updateVersion) return; // Stale update.
                Contents = emoticonContents;
                TimelineContents = new TimelineContentsViewModel(Contents);
            });
        }
        catch { } // Credential/URL failures keep the text placeholder.
    }

    private async Task AttachInteractionsAsync(int version)
    {
        try
        {
            // Fetch both share and UP (sympathy) user lists in parallel when available.
            Task<List<ShareData.Share>> sharesTask = HasSharedUsers ? App.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.GetShares(_postData, false, null)) : Task.FromResult<List<ShareData.Share>>([]);
            Task<List<ShareData.Share>> sympathiesTask = HasRepostedUsers ? App.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.GetShares(_postData, true, null)) : Task.FromResult<List<ShareData.Share>>([]);
            await Task.WhenAll(sharesTask, sympathiesTask);

            var shares = sharesTask.Result ?? [];
            var sympathies = sympathiesTask.Result ?? [];

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (version != _updateVersion) return; // Stale update.

                // Mirror HistoryPostViewModel: reactions.Concat(shares).Concat(reposts).OrderByDescending(CreatedAt).
                Interactions = [.. (Interactions ?? [])
                    .Concat(shares.Select(x => new KakaoInteractionViewModel(x, InteractionType.Share)))
                    .Concat(sympathies.Select(x => new KakaoInteractionViewModel(x, InteractionType.Repost)))
                    .OrderByDescending(x => x.CreatedAt)];
            });
        }
        catch { } // Credential/URL failures keep the existing reaction-only list.
    }

    private static List<IContentViewModel> GenerateContentViewModels(PostData postData, PostType postType)
    {
        var contents = new List<IContentViewModel>();

        if (postData.content_decorators is { Count: > 0 }) contents.Add(new TextTypeContentsViewModel(postData.content_decorators, postType));
        else if (!string.IsNullOrWhiteSpace(postData.content)) contents.Add(new TextTypeContentsViewModel(KakaoStoryUtils.GetQuoteDataFromString(postData.content), postType));

        if (postData.scrap != null) contents.Add(new ExternalUrlContentViewModel(postData.scrap));

        if (postData.media is { Count: > 0 }) contents.Add(new KakaoWrappedMediaContentsViewModel(postData.media, postType));

        return contents;
    }

    public override async Task HandleReactionAsync()
    {
        HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);

        // Delete reaction
        if (_postData.liked)
        {
            await App.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.LikePost(_postData.id, null));
            await RefreshAsync();
            return;
        }

        // Add reaction
        var rawReaction = await App.Page.DisplayActionSheetAsync("느낌 달기", Constants.PromptCancel, null, "좋아요", "멋져요", "기뻐요", "슬퍼요", "힘내요");
        if (rawReaction == null || rawReaction == Constants.PromptCancel) return;

        var emotion = rawReaction switch
        {
            "좋아요" => "like",
            "멋져요" => "good",
            "기뻐요" => "pleasure",
            "슬퍼요" => "sad",
            "힘내요" => "cheerup",
            _ => null
        };
        if (emotion == null) return;

        await App.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.LikePost(_postData.id, emotion));
        await RefreshAsync();
    }

    public override async Task<Result> RefreshAsync()
    {
        var post = await App.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.GetPost(_postData.id));
        if (post == null) return Result.Failure(ErrorType.NotFound, "카카오스토리 게시글을 불러오지 못했습니다.");

        WeakReferenceMessenger.Default.Send(new ValueChangedMessage<PostData>(post));
        return Result.Success();
    }

    public override async Task DeleteAsync(bool popModal)
    {
        if (!IsMyPost)
        {
            await App.Page.DisplayAlertAsync("권한 부족", "삭제할 수 없는 게시글입니다.", Constants.PromptOk);
            return;
        }

        var confirm = await App.Page.DisplayAlertAsync("게시글 삭제", "정말로 게시글을 삭제하시겠습니까?", Constants.PromptOk, Constants.PromptCancel);
        if (!confirm) return;

        try
        {
            await App.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.DeletePost(_postData.id));
            WeakReferenceMessenger.Default.Send(new ValueDeletedMessage<PostData>(_postData));
            if (popModal) await App.PopAsync();
        }
        catch (Exception exception)
        {
            await App.Page.DisplayAlertAsync("오류", $"게시글 삭제에 실패하였습니다.\n{exception.Message}", Constants.PromptOk);
        }
    }

    public override async Task DisplayActionSheetAsync(bool popModal)
    {
        var options = new List<string>
        {
            _postData.sympathized ? "UP 해제" : "UP",
            _postData.bookmarked ? "관심글 삭제" : "관심글로 저장"
        };
        if (_postData.sharable) options.Add("게시글 공유");
        if (IsMyPost)
        {
            if(_postData.modifiable) options.Add("게시글 수정");

            options.Add("공개범위 설정");
            options.Add("게시글 삭제");
        }
        else options.Add("이 글 숨기기");

        var action = await App.Page.DisplayActionSheetAsync("카카오스토리 게시물 옵션", Constants.PromptCancel, null, [.. options]);
        if (action == null || action == Constants.PromptCancel) return;

        if (action is "UP" or "UP 해제") await HandleRepostAsync();
        else if (action == "게시글 공유") await HandleShareAsync();
        else if (action is "관심글로 저장" or "관심글 삭제") await HandleBookmarkAsync();
        else if (action == "공개범위 설정") await HandleChangePermissionAsync();
        else if (action == "게시글 수정") await HandleEditAsync();
        else if (action == "이 글 숨기기") await HandleHidePostAsync(popModal);
        else if (action == "게시글 삭제") await DeleteAsync(popModal);
    }

    private async Task HandleChangePermissionAsync()
    {
        // Kakao Story supports only A(All)/F(Friends)/M(OnlyMe) — a separate action sheet.
        var options = new List<string>
        {
            $"전체 공개{( _postData.permission == "A" ? " (현재)" : string.Empty)}",
            $"친구 공개{( _postData.permission == "F" ? " (현재)" : string.Empty)}",
            $"나만 보기{( _postData.permission == "M" ? " (현재)" : string.Empty)}"
        };

        var action = await App.Page.DisplayActionSheetAsync("공개범위 설정", Constants.PromptCancel, null, [.. options]);
        if (action == null || action == Constants.PromptCancel) return;

        var permission = action switch
        {
            string option when option.StartsWith("전체 공개") => "A",
            string option when option.StartsWith("친구 공개") => "F",
            string option when option.StartsWith("나만 보기") => "M",
            _ => null
        };
        if (permission == null || permission == _postData.permission) return;

        try
        {
            // preserve sharable/comment writable state; only the permission changes
            await App.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.SetActivityProfile(_postData.id, permission, _postData.sharable, _postData.comment_all_writable, false));
            await RefreshAsync();
        }
        catch (Exception exception)
        {
            await App.Page.DisplayAlertAsync("오류", $"공개범위 변경에 실패하였습니다.\n{exception.Message}", Constants.PromptOk);
        }
    }

    private async Task HandleHidePostAsync(bool popModal)
    {
        var confirm = await App.Page.DisplayAlertAsync("이 글 숨기기", "이 글을 숨기면 타임라인에서 더 이상 보이지 않습니다. 계속하시겠습니까?", Constants.PromptOk, Constants.PromptCancel);
        if (!confirm) return;

        try
        {
            await App.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.HidePost(_postData.id));
            WeakReferenceMessenger.Default.Send(new ValueDeletedMessage<PostData>(_postData));
            if (popModal) await App.PopAsync();
        }
        catch (Exception exception)
        {
            await App.Page.DisplayAlertAsync("오류", $"게시글 숨기기에 실패하였습니다.\n{exception.Message}", Constants.PromptOk);
        }
    }

    private async Task HandleBookmarkAsync()
    {
        HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);

        // PinPost toggles: currently bookmarked -> DELETE, otherwise -> POST.
        var isUnpin = _postData.bookmarked;
        try
        {
            await App.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.PinPost(_postData.id, isUnpin));
            await RefreshAsync();
        }
        catch (Exception exception) { await App.Page.DisplayAlertAsync("오류", $"관심글 처리에 실패하였습니다.\n{exception.Message}", Constants.PromptOk); }
    }

    private async Task HandleEditAsync()
    {
        var page = new EditPostPage(_postData, true);
        await App.PushAsync(page);
    }

    public override async Task HandleTapAsync()
    {
        // Tapping the embedded original post card opens the original post (KSMP pattern).
        if (IsParentPost)
        {
            var originalPost = await App.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.GetPost(_postData.id));
            if (originalPost == null)
            {
                await App.Page.DisplayAlertAsync("안내", "원본 게시글을 불러올 수 없습니다.", Constants.PromptOk);
                return;
            }

            var originalViewModel = new KakaoPostViewModel(originalPost, PostType.Unwrapped);
            var originalPostPage = new PostPage(originalViewModel);
            await App.PushAsync(originalPostPage);
            return;
        }

        var result = await RefreshAsync();
        if (result.IsFailure) return;

        var newViewModel = new KakaoPostViewModel(_postData, PostType.Unwrapped);
        var postPage = new PostPage(newViewModel);
        await App.PushAsync(postPage);
    }

    public override async Task HandleProfileTapAsync()
    {
        if (_postData.actor?.id == null)
        {
            await App.Page.DisplayAlertAsync("안내", "프로필을 불러올 수 없습니다.", Constants.PromptOk);
            return;
        }

        var profilePage = new UserPage(_postData.actor.id, true);
        await App.PushAsync(profilePage);
    }

    public override async Task HandleShareAsync()
    {
        if (!_postData.sharable)
        {
            await App.Page.DisplayAlertAsync("안내", "이 게시글은 공유할 수 없는 게시글입니다.", Constants.PromptOk);
            return;
        }

        if (_postData.@object != null)
        {
            await App.Page.DisplayAlertAsync("안내", "공유된 게시글은 공유할 수 없습니다.", Constants.PromptOk);
            return;
        }

        var page = new EditPostPage(_postData);
        await App.PushAsync(page);
    }

    public override async Task HandleRepostAsync()
    {
        HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);

        var isUp = _postData.sympathized;
        try
        {
            await App.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.UpPost(_postData.id, isUp));
            await RefreshAsync();
        }
        catch (Exception exception)
        {
            await App.Page.DisplayAlertAsync("오류", $"UP 처리에 실패하였습니다.\n{exception.Message}", Constants.PromptOk);
        }
    }

    public override async Task HandleMoreTapAsync() => await DisplayActionSheetAsync(false);

    // Reaction (느낌) — see HandleReactionAsync above.
    // Share count displays share_count - sympathy_count (Kakao includes UP actions in share_count).
    // Mirror HistoryPostViewModel.HandleReactionTapAsync/HandleSharedTapAsync/HandleRepostTapAsync:
    // use the already-merged Interactions list instead of fetching share/UP lists again.
    public override async Task HandleReactionTapAsync()
    {
        var viewModels = Interactions
            .Where(x => x is KakaoInteractionViewModel interaction && interaction.Type == InteractionType.Reaction)
            .Select(x => new KakaoFriendshipViewModel(((KakaoInteractionViewModel)x).Share));
        var page = new InteractionsPage(viewModels, InteractionType.Reaction);
        await App.PushAsync(page);
    }

    public override async Task HandleSharedTapAsync()
    {
        var viewModels = Interactions
            .Where(x => x is KakaoInteractionViewModel interaction && interaction.Type == InteractionType.Share)
            .Select(x => new KakaoFriendshipViewModel(((KakaoInteractionViewModel)x).Share, (KakaoInteractionViewModel)x));
        var page = new InteractionsPage(viewModels, InteractionType.Share);
        await App.PushAsync(page);
    }

    public override async Task HandleRepostTapAsync()
    {
        var viewModels = Interactions
            .Where(x => x is KakaoInteractionViewModel interaction && interaction.Type == InteractionType.Repost)
            .Select(x => new KakaoFriendshipViewModel(((KakaoInteractionViewModel)x).Share));
        var page = new InteractionsPage(viewModels, InteractionType.Repost);
        await App.PushAsync(page);
    }

    public override async Task HandleLoadMoreComments()
    {
        var oldestViewModel = Comments.OfType<KakaoCommentViewModel>().FirstOrDefault();
        if (oldestViewModel == null) return;

        var comments = await App.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.GetComments(_postData.id, oldestViewModel.Comment.id));
        if (comments == null) return;

        // The API returns newest-first; reverse to oldest-first so prepending keeps chronological order.
        comments.Reverse();
        var existingIds = Comments.OfType<KakaoCommentViewModel>().Select(x => x.Comment.id).ToHashSet();
        var commentViewModels = comments.Where(x => !existingIds.Contains(x.id)).Select(x => new KakaoCommentViewModel(x, PostType, this));
        foreach (var commentViewModel in commentViewModels) Comments.Insert(0, commentViewModel);
        HasMoreComments = CommentsCount > Comments.Count;
    }
}
