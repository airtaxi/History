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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.Storage.Pickers;
using Windows.ApplicationModel.DataTransfer;
using static History.Commons.KakaoStory.KakaoStoryApiHandler.DataType;
using static History.Commons.KakaoStory.KakaoStoryApiHandler.DataType.CommentData;

namespace History.WindowsClient.ViewModels;

// Kakao Story post view model: fills the shared post surface from the activity DTO.
// Bundled feeds (share/UP activities) are unwrapped before construction
// (see KakaoStoryUtils.CreatePostViewModel); an embedded original renders as ParentPost.
public partial class KakaoPostViewModel : BasePostViewModel, IRecipient<ValueChangedMessage<PostData>>
{
    private const string SaveAsFileText = "파일로 저장";
    private const string SaveToClipboardText = "클립보드로 저장";

    // The five Kakao Story emotions in the display order of the reaction menu.
    private static readonly (string Emotion, string DisplayName)[] ReactionOptions =
    [
        ("like", "좋아요"),
        ("good", "멋져요"),
        ("pleasure", "기뻐요"),
        ("sad", "슬퍼요"),
        ("cheerup", "힘내요"),
    ];

    private PostData _postData;

    public PostData PostData => _postData;
    public bool IsMyPost => _postData.actor?.id == CommonShared.KakaoUserId;

    protected PostData CurrentPostData
    {
        get => _postData;
        set => _postData = value;
    }

    public KakaoPostViewModel(PostData postData, PostType postType, BaseViewModel baseViewModel, bool isParentPost = false) : base(postType, isParentPost, baseViewModel)
    {
        RepostCountPrefix = "UP ";
        UpdatePost(postData ?? throw new Exception("[KakaoPostViewModel] POST IS NULL"));

        WeakReferenceMessenger.Default.Register(this);
    }

    public void Receive(ValueChangedMessage<PostData> message)
    {
        if (message.Value.id != _postData.id) return;

        UpdatePost(message.Value);
    }

    protected virtual void UpdatePost(PostData postData)
    {
        _postData = postData;

        var actor = postData.actor;

        Nickname = actor?.display_name;
        IsModerator = false;
        IsAdmin = false;
        ProfileThumbnailImageSource = actor?.profile_image_url != null ? new BitmapImage(new Uri(actor.profile_image_url)) : null;

        Contents = PostHelper.GenerateContentViewModels(BuildBaseContents(postData), PostType, BaseViewModel, IsParentPost, postData.id);

        // Kakao Story embeds the original post in @object for share/UP activities. The embedded
        // post renders as a nested card; a post from a banned author is skipped so only the
        // share text remains.
        ParentPost = postData.@object?.actor?.relation?.ban != "A" && postData.@object != null ? new KakaoPostViewModel(postData.@object, PostType, BaseViewModel, true) : null;
        IsRepost = false;
        IsShare = postData.@object != null;

        // Kakao's share count includes sympathy (UP) actions, so the share count is the remainder.
        var shareCount = Math.Max(0, postData.share_count - postData.sympathy_count);
        HasSharedUsers = shareCount > 0;
        SharedUsersCount = shareCount;
        HasReactions = postData.like_count > 0;
        ReactionsCount = postData.like_count;
        HasRepostedUsers = postData.sympathy_count > 0;
        RepostedUsersCount = postData.sympathy_count;
        IsReposted = postData.sympathized;
        HasInteractions = HasReactions || HasSharedUsers || HasRepostedUsers;

        Interactions = postData.likes?.OrderByDescending(x => x.created_at).Select(x => (BaseInteractionViewModel)new KakaoInteractionViewModel(x, BaseViewModel)).ToList() ?? [];
        ReactionUsers = postData.likes?.OrderByDescending(x => x.created_at).Select(x => (BaseFriendshipViewModel)new KakaoFriendshipViewModel(x, BaseViewModel, new KakaoInteractionViewModel(x, BaseViewModel))).ToList() ?? [];
        // Share and UP user lists load on demand when their flyout opens.
        SharedUsers = [];
        RepostedUsers = [];

        var reactionVisual = KakaoStoryUtils.GetEmotionVisual(postData.liked ? postData.liked_emotion : null);
        Reaction = null;
        ReactionGlyph = reactionVisual.Glyph;
        ReactionBrush = new SolidColorBrush(reactionVisual.Color);

        var sourceComments = postData.comments ?? postData.latest_comments ?? [];
        Comments = [.. sourceComments.Where(c => c.writer?.relation?.ban != "A").Select(c => new KakaoCommentViewModel(c, PostType, this)).OrderBy(x => x.CreatedAt)];
        LatestComment = Comments.LastOrDefault();
        CommentsCount = postData.comment_count;
        HasComments = CommentsCount > 0;
        HasNoComments = CommentsCount == 0;
        HasMoreComments = CommentsCount > Comments.Count;

        CreatedAt = postData.created_at;
        // content_updated_at exists only when the content was actually edited.
        ModifiedAt = postData.content_updated_at.Year > 1 ? postData.content_updated_at : null;
        TimestampText = KakaoStoryUtils.GetTimeString(postData.created_at, ModifiedAt);

        // Kakao Story permission mapping: M = Only me, F = Friends, A = Everyone.
        DiscoveryOptionGlyph = postData.permission switch
        {
            "M" => "\uE72E",
            "F" => "\uE716",
            "A" => "\uE774",
            _ => "\uE9CE",
        };

        IsNotificationsMuted = postData.push_mute;
        HasUnreadNotification = postData.has_unread_reaction;
    }

    public override string ProfileMediaUri => _postData.actor?.profile_image_url;

    public override List<BaseContent> GetRenderRawContents() => BuildBaseContents(_postData);

    // Converts the Kakao post into BaseContent for the shared content pipeline: text
    // decorators (emoticons preserved as signed sticker URLs), scrap card and media.
    private static List<BaseContent> BuildBaseContents(PostData postData)
    {
        var contents = new List<BaseContent>();

        if (postData.content_decorators is { Count: > 0 }) contents.AddRange(KakaoStoryUtils.ConvertToBaseContents(postData.content_decorators, true));
        else if (!string.IsNullOrWhiteSpace(postData.content)) contents.AddRange(KakaoStoryUtils.ConvertToBaseContents(KakaoStoryUtils.GetQuoteDataFromString(postData.content), true));

        if (postData.scrap != null) contents.Add(new ExternalUrlContent { Title = postData.scrap.title, Description = postData.scrap.description, SourceUrl = postData.scrap.dest_url ?? postData.scrap.url, ThumbnailImageUrl = postData.scrap.image?.FirstOrDefault() });

        if (postData.media is { Count: > 0 })
        {
            foreach (var medium in postData.media)
            {
                var isVideo = medium.content_type?.StartsWith("video", StringComparison.OrdinalIgnoreCase) == true;

                // Detail and full-screen surfaces serve the original image (HQ video for playback)
                // so they stay sharp on large surfaces; wrapped surfaces keep the display copy and
                // videos keep their preview frame.
                var detailUrl = isVideo ? medium.url_hq ?? medium.url ?? medium.url2 ?? medium.thumbnail_url : medium.origin_url ?? medium.url ?? medium.url2 ?? medium.thumbnail_url;
                var displayUrl = medium.url ?? medium.url2 ?? medium.thumbnail_url;
                var thumbnail = medium.preview_url ?? medium.preview_url_hq ?? medium.thumbnail_url ?? medium.url;
                var caption = medium.caption?.FirstOrDefault(x => x.type == "text")?.text;
                contents.Add(new MediaContent
                {
                    MediaId = detailUrl,
                    ThumbnailMediaId = isVideo ? thumbnail : displayUrl,
                    MimeType = isVideo ? "video/mp4" : "image/jpeg",
                    Description = caption
                });
            }
        }

        return contents;
    }

    public override void PopulateMoreMenuFlyout(MenuFlyout menuFlyout)
    {
        menuFlyout.Items.Clear();

        menuFlyout.Items.Add(Utils.CreateActionItem(_postData.sympathized ? "UP 해제" : "UP", "\uE8EB", HandleRepostAsync));
        if (_postData.bookmarked) menuFlyout.Items.Add(Utils.CreateActionItem("관심글 삭제", "\uE8A4", HandleBookmarkAsync));
        else menuFlyout.Items.Add(Utils.CreateActionItem("관심글로 저장", "\uE8A4", HandleBookmarkAsync));

        if (_postData.sharable) menuFlyout.Items.Add(Utils.CreateActionItem("게시글 공유", "\uE72D", HandleShareAsync));

        if (IsMyPost)
        {
            if (_postData.modifiable) menuFlyout.Items.Add(Utils.CreateActionItem("게시글 수정", "\uE70F", HandleEditPostAsync));

            // Kakao Story supports only A(All)/F(Friends)/M(OnlyMe).
            var permissionSubItem = new MenuFlyoutSubItem { Text = "공개범위 설정" };
            permissionSubItem.Items.Add(Utils.CreateActionItem($"전체 공개{(_postData.permission == "A" ? " (현재)" : string.Empty)}", "\uE774", () => HandleChangePermissionAsync("A")));
            permissionSubItem.Items.Add(Utils.CreateActionItem($"친구 공개{(_postData.permission == "F" ? " (현재)" : string.Empty)}", "\uE716", () => HandleChangePermissionAsync("F")));
            permissionSubItem.Items.Add(Utils.CreateActionItem($"나만 보기{(_postData.permission == "M" ? " (현재)" : string.Empty)}", "\uE72E", () => HandleChangePermissionAsync("M")));
            menuFlyout.Items.Add(permissionSubItem);

            menuFlyout.Items.Add(Utils.CreateActionItem("게시글 삭제", "\uE74D", DeleteAsync));
        }
        else
        {
            menuFlyout.Items.Add(Utils.CreateActionItem(IsNotificationsMuted ? "이 글 알림 받기" : "이 글 알림 안받기", IsNotificationsMuted ? "\uEA8F" : "\uE7ED", HandleMuteNotificationsAsync));
            menuFlyout.Items.Add(Utils.CreateActionItem("이 글 숨기기", "\uE7B3", HandleHidePostAsync));
        }

        menuFlyout.Items.Add(Utils.CreateActionItem("게시글 URL 복사", "\uE71B", HandleCopyUrl));
        menuFlyout.Items.Add(Utils.CreateActionItem("게시글 이미지로 저장", "\uEE71", HandleSaveImageAsync));
        menuFlyout.Items.Add(Utils.CreateActionItem("게시글 본문만 이미지로 저장", "\uE7C3", HandleSaveBodyImageAsync));
    }

    public override void PopulateReactionMenuFlyout(MenuFlyout menuFlyout)
    {
        menuFlyout.Items.Clear();

        // Delete the existing reaction first; the emotion picker is for new reactions only.
        if (_postData.liked)
        {
            menuFlyout.Items.Add(Utils.CreateActionItem("느낌 취소", "\uEA92", () => HandleReactionTypedAsync(null)));
            return;
        }

        foreach (var (emotion, displayName) in ReactionOptions)
        {
            var reactionVisual = KakaoStoryUtils.GetEmotionVisual(emotion);
            menuFlyout.Items.Add(Utils.CreateActionItem(displayName, reactionVisual.Glyph, () => HandleReactionTypedAsync(emotion), reactionVisual.Color));
        }
    }

    public override async Task LoadSharedUsersAsync()
    {
        if (SharedUsers.Count > 0) return;

        var shares = await BaseViewModel.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.GetShares(_postData, false, null));
        SharedUsers = shares?.Select(x => (BaseFriendshipViewModel)new KakaoFriendshipViewModel(x, BaseViewModel, new KakaoInteractionViewModel(x, BaseViewModel, InteractionType.Share))).ToList() ?? [];
    }

    public override async Task LoadRepostedUsersAsync()
    {
        if (RepostedUsers.Count > 0) return;

        var sympathies = await BaseViewModel.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.GetShares(_postData, true, null));
        RepostedUsers = sympathies?.Select(x => (BaseFriendshipViewModel)new KakaoFriendshipViewModel(x, BaseViewModel, new KakaoInteractionViewModel(x, BaseViewModel, InteractionType.Repost))).ToList() ?? [];
    }

    public override async Task<Result> RefreshAsync()
    {
        var post = await BaseViewModel.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.GetPost(_postData.id));
        if (post == null) return Result.Failure(ErrorType.NotFound, "카카오스토리 게시글을 불러오지 못했습니다.");

        WeakReferenceMessenger.Default.Send(new ValueChangedMessage<PostData>(post));
        return Result.Success();
    }

    public override async Task HandleLoadMoreComments()
    {
        var oldestViewModel = Comments.OfType<KakaoCommentViewModel>().FirstOrDefault();
        if (oldestViewModel == null) return;

        var comments = await BaseViewModel.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.GetComments(_postData.id, oldestViewModel.Comment.id));
        if (comments == null) return;

        // The API returns newest-first; reverse to oldest-first so prepending keeps chronological order.
        comments.Reverse();
        var existingIds = Comments.OfType<KakaoCommentViewModel>().Select(x => x.Comment.id).ToHashSet();
        var commentViewModels = comments.Where(x => !existingIds.Contains(x.id) && x.writer?.relation?.ban != "A").Select(x => new KakaoCommentViewModel(x, PostType, this));
        foreach (var commentViewModel in commentViewModels) Comments.Insert(0, commentViewModel);
        HasMoreComments = CommentsCount > Comments.Count;
    }

    public override async Task HandleTapAsync()
    {
        // Tapping the embedded original post card opens the original post.
        if (IsParentPost)
        {
            var originalPost = await BaseViewModel.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.GetPost(_postData.id));
            if (originalPost == null)
            {
                await BaseViewModel.ShowMessageDialogAsync(new("안내", "원본 게시글을 불러올 수 없습니다."));
                return;
            }

            WeakReferenceMessenger.Default.Send(new ValueChangedMessage<PostData>(originalPost));
            BaseViewModel.RequestNavigation(typeof(PostPage), originalPost);
            return;
        }

        // The detail page opens with the freshly fetched post so the full comment list is available.
        var result = await RefreshAsync();
        if (result.IsFailure) return;

        BaseViewModel.RequestNavigation(typeof(PostPage), _postData);
    }

    public override void HandleProfileTap()
    {
        if (_postData.actor?.id == null) return;

        BaseViewModel.RequestNavigation(typeof(ProfilePage), new KakaoProfileParameters(_postData.actor.id));
    }

    public override async Task HandleShareAsync()
    {
        if (!_postData.sharable)
        {
            await BaseViewModel.ShowMessageDialogAsync(new("안내", "이 게시글은 공유할 수 없는 게시글입니다."));
            return;
        }

        if (_postData.@object != null)
        {
            await BaseViewModel.ShowMessageDialogAsync(new("안내", "공유된 게시글은 공유할 수 없습니다."));
            return;
        }

        var composePostWindow = new ComposePostWindow(new ComposePostWindowViewModel(App.Services.GetRequiredService<ApplicationSettings>(), _postData, isKakaoEdit: false));
        composePostWindow.MakeModal(MainWindow.Instance);
    }

    // UP (sympathy) toggles: currently sympathized -> DELETE, otherwise -> POST.
    public override async Task HandleRepostAsync()
    {
        var isUp = _postData.sympathized;
        try
        {
            await BaseViewModel.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.UpPost(_postData.id, isUp));
            await RefreshAsync();
        }
        catch (Exception exception) { await BaseViewModel.ShowMessageDialogAsync(new(Constants.ErrorTitle, $"UP 처리에 실패하였습니다.\n{exception.Message}")); }
    }

    public override async Task HandleMuteNotificationsAsync()
    {
        var isMuting = !IsNotificationsMuted;
        var confirm = await BaseViewModel.ShowMessageDialogAsync(new("알림 설정", isMuting ? "이 글의 알림을 끄시겠습니까?" : "이 글의 알림을 다시 받으시겠습니까?", DialogHelper.DefaultOkButtonText, DialogHelper.DefaultCancelButtonText));
        if (confirm != ContentDialogResult.Primary) return;

        try
        {
            await BaseViewModel.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.MutePost(_postData.id, isMuting));
            await RefreshAsync();
        }
        catch (Exception exception) { await BaseViewModel.ShowMessageDialogAsync(new(Constants.ErrorTitle, $"알림 설정 변경에 실패하였습니다.\n{exception.Message}")); }
    }

    // Tapping the "OO님이 UP 했어요" attribution opens the original author's profile.
    public override void HandleRepostedUserTap()
    {
        var repostedUserId = _postData.bundled_feed?.original_activity?.actor?.id;
        if (repostedUserId == null) return;

        BaseViewModel.RequestNavigation(typeof(ProfilePage), new KakaoProfileParameters(repostedUserId));
    }

    private async Task HandleReactionTypedAsync(string emotion)
    {
        try
        {
            await BaseViewModel.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.LikePost(_postData.id, emotion));
            await RefreshAsync();
        }
        catch (Exception exception) { await BaseViewModel.ShowMessageDialogAsync(new(Constants.ErrorTitle, $"느낌 반영에 실패하였습니다.\n{exception.Message}")); }
    }

    private async Task HandleBookmarkAsync()
    {
        // PinPost toggles: currently bookmarked -> DELETE, otherwise -> POST.
        var isUnpin = _postData.bookmarked;
        try
        {
            await BaseViewModel.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.PinPost(_postData.id, isUnpin));
            await RefreshAsync();
        }
        catch (Exception exception) { await BaseViewModel.ShowMessageDialogAsync(new(Constants.ErrorTitle, $"관심글 처리에 실패하였습니다.\n{exception.Message}")); }
    }

    private async Task HandleChangePermissionAsync(string permission)
    {
        if (permission == _postData.permission)
        {
            await BaseViewModel.ShowMessageDialogAsync(new("안내", "이미 선택된 공개범위입니다."));
            return;
        }

        try
        {
            // Preserve the sharable/comment writable state; only the permission changes.
            await BaseViewModel.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.SetActivityProfile(_postData.id, permission, _postData.sharable, _postData.comment_all_writable, false));
            await RefreshAsync();
        }
        catch (Exception exception) { await BaseViewModel.ShowMessageDialogAsync(new(Constants.ErrorTitle, $"공개범위 변경에 실패하였습니다.\n{exception.Message}")); }
    }

    private async Task HandleHidePostAsync()
    {
        var confirm = await BaseViewModel.ShowMessageDialogAsync(new("이 글 숨기기", "이 글을 숨기면 타임라인에서 더 이상 보이지 않습니다. 계속하시겠습니까?", DialogHelper.DefaultOkButtonText, DialogHelper.DefaultCancelButtonText));
        if (confirm != ContentDialogResult.Primary) return;

        try
        {
            await BaseViewModel.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.HidePost(_postData.id));
            WeakReferenceMessenger.Default.Send(new ValueDeletedMessage<PostData>(_postData));
        }
        catch (Exception exception) { await BaseViewModel.ShowMessageDialogAsync(new(Constants.ErrorTitle, $"게시글 숨기기에 실패하였습니다.\n{exception.Message}")); }
    }

    // Opens the composer prefilled with the post; a successful edit refreshes this post so
    // every surface showing it updates in place.
    private Task HandleEditPostAsync()
    {
        var composePostWindow = new ComposePostWindow(new ComposePostWindowViewModel(App.Services.GetRequiredService<ApplicationSettings>(), _postData, isKakaoEdit: true));
        composePostWindow.ViewModel.SubmitCompleted += OnKakaoPostEditSubmitCompleted;
        composePostWindow.MakeModal(MainWindow.Instance);
        return Task.CompletedTask;
    }

    private async void OnKakaoPostEditSubmitCompleted(object sender, EventArgs e) => await RefreshAsync();

    private async Task DeleteAsync()
    {
        if (!IsMyPost)
        {
            await BaseViewModel.ShowMessageDialogAsync(new("권한 부족", "삭제할 수 없는 게시글입니다."));
            return;
        }

        var confirm = await BaseViewModel.ShowMessageDialogAsync(new("게시글 삭제", "정말로 게시글을 삭제하시겠습니까?", DialogHelper.DefaultOkButtonText, DialogHelper.DefaultCancelButtonText));
        if (confirm != ContentDialogResult.Primary) return;

        try
        {
            await BaseViewModel.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.DeletePost(_postData.id));
            WeakReferenceMessenger.Default.Send(new ValueDeletedMessage<PostData>(_postData));
        }
        catch (Exception exception) { await BaseViewModel.ShowMessageDialogAsync(new(Constants.ErrorTitle, $"게시글 삭제에 실패하였습니다.\n{exception.Message}")); }
    }

    private async Task HandleCopyUrl()
    {
        if (_postData.permalink == null)
        {
            await BaseViewModel.ShowMessageDialogAsync(new("안내", "이 게시글은 URL이 존재하지 않습니다."));
            return;
        }

        var dataPackage = new DataPackage { RequestedOperation = DataPackageOperation.Copy };
        dataPackage.SetText(_postData.permalink);
        Clipboard.SetContent(dataPackage);
        await BaseViewModel.ShowMessageDialogAsync(new("안내", "게시글 URL이 클립보드에 복사되었습니다."));
    }

    private async Task HandleSaveImageAsync()
    {
        var confirm = await BaseViewModel.ShowMessageDialogAsync(new("게시글 이미지로 저장", "이 게시글을 이미지로 저장하시겠습니까?", "확인", "취소"));
        if (confirm != ContentDialogResult.Primary) return;

        var saveTarget = await ConfirmSaveTargetAsync();
        if (saveTarget == null) return;

        var includeComments = await ConfirmIncludeCommentsAsync();
        await SavePostImageAsync(this, includeComments ? Comments : null, saveTarget == SaveToClipboardText);
    }

    // Saves only the post contents without the profile header (profile image, nickname, timestamp).
    // The shared (parent) post card, when present, is still included because it is part of the body.
    private async Task HandleSaveBodyImageAsync()
    {
        var saveTarget = await ConfirmSaveTargetAsync();
        if (saveTarget == null) return;

        await SavePostImageAsync(this, null, saveTarget == SaveToClipboardText, includeHeader: false);
    }

    // Asks whether the rendered image should go to the clipboard or to a file.
    private async Task<string> ConfirmSaveTargetAsync() => await BaseViewModel.ShowSelectionDialogAsync("저장 방식 선택", [SaveAsFileText, SaveToClipboardText]);

    private async Task SavePostImageAsync(BasePostViewModel post, IEnumerable<BaseCommentViewModel> comments, bool saveToClipboard, bool includeHeader = true)
    {
        var renderBytes = await BaseViewModel.ExecuteWithLoadingAsync(async () => await PostImageRendererHelper.RenderAsync(BuildBaseContents(_postData), post, comments, includeHeader: includeHeader));
        if (renderBytes == null)
        {
            await BaseViewModel.ShowMessageDialogAsync(new(Constants.ErrorTitle, "이미지로 저장할 내용이 없습니다."));
            return;
        }

        if (saveToClipboard)
        {
            await Utils.CopyPngBytesToClipboardAsync(renderBytes);
            await BaseViewModel.ShowMessageDialogAsync(new("안내", "게시글 이미지가 클립보드에 복사되었습니다."));
            return;
        }

        var saveResult = await BaseViewModel.SaveFileAsync(new FileSavePickerParameters(new Dictionary<string, IReadOnlyList<string>> { ["PNG 이미지"] = [".png"] }, $"post_{DateTime.Now:yyyyMMdd_HHmmss}.png", ".png", PickerLocationId.PicturesLibrary));
        if (saveResult == null) return;

        await File.WriteAllBytesAsync(saveResult.Path, renderBytes);
        await BaseViewModel.ShowMessageDialogAsync(new("안내", "게시글 이미지가 저장되었습니다."));
    }

    private async Task<bool> ConfirmIncludeCommentsAsync()
    {
        if (Comments.Count == 0) return false;
        return await BaseViewModel.ShowMessageDialogAsync(new("게시글 이미지로 저장", $"댓글 {Comments.Count}개를 포함해서 저장하시겠습니까?", "포함", "취소")) == ContentDialogResult.Primary;
    }
}
