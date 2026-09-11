using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using History.Commons;
using History.Commons.Api.Post;
using History.Commons.DataTypes.Contents;
using History.Commons.Enums;
using History.Commons.KakaoStory;
using History.WindowsClient.Dialogs;
using History.WindowsClient.Helpers;
using History.WindowsClient.Messages;
using History.WindowsClient.Models;
using Microsoft.UI.Xaml.Controls;
using static History.Commons.KakaoStory.KakaoStoryApiHandler.DataType;
using static History.Commons.KakaoStory.KakaoStoryApiHandler.DataType.CommentData;

namespace History.WindowsClient.ViewModels;

// Kakao Story surface of the composer: Kakao Story mode composes a new post that is
// mirrored to History, while the post menu opens the composer to edit or share an
// existing Kakao Story post; History posts can also be mirrored through the cross-post
// toggle.
public sealed partial class ComposePostWindowViewModel : BaseViewModel
{
    private const string KakaoStoryProfanityCheckEnabledKey = "KakaoStoryProfanityCheckEnabled";

    private readonly PostData _kakaoPost;
    private readonly bool _isKakaoWriteMode;
    private readonly bool _isKakaoEditMode;
    private readonly bool _isKakaoShareMode;
    private bool _isSuppressingKakaoPostEnabledPersist;

    public bool IsKakaoMode => _isKakaoWriteMode || _isKakaoEditMode || _isKakaoShareMode;

    public bool IsKakaoWriteMode => _isKakaoWriteMode;

    public bool IsKakaoEditMode => _isKakaoEditMode;

    public bool IsKakaoShareMode => _isKakaoShareMode;

    // History-only surfaces (reservation, poll, comment permission, cross-post toggle)
    // stay hidden while the composer edits or shares a Kakao Story post.
    public bool IsHistoryMode => !IsKakaoMode;

    // Kakao Story share is text-only, so the media/sticker/URL buttons are hidden there.
    public bool IsMediaAttachVisible => !_isKakaoShareMode;

    public bool IsKakaoCommentWritableVisible => _isKakaoEditMode;

    public bool IsKakaoCrossPostToggleVisible => IsHistoryMode && !IsEditMode && !IsShareMode && KakaoStoryFeatureGateHelper.IsEnabled;

    // Editor prefill for a Kakao Story post edit.
    public List<BaseContent> KakaoEditorContents { get; } = [];

    // Kakao Story comment writable flag (comment_all_writable).
    [ObservableProperty]
    public partial bool IsKakaoCommentWritable { get; set; }

    // Kakao Story post edit/share entry point of the composer. Editing prefills the editor
    // from the post decorators and keeps the server media and the scrap card; sharing
    // composes a new post attached to the origin.
    public ComposePostWindowViewModel(ApplicationSettings settings, PostData kakaoPost, bool isKakaoEdit) : this(settings)
    {
        _kakaoPost = kakaoPost ?? throw new Exception("[ComposePostWindowViewModel] KAKAO POST IS NULL");
        _isKakaoEditMode = isKakaoEdit;
        _isKakaoShareMode = !isKakaoEdit;

        var permissionOption = _kakaoPost.permission switch
        {
            "M" => DiscoveryOption.OnlyMe,
            "A" => DiscoveryOption.Everyone,
            _ => DiscoveryOption.Friends,
        };
        InitializeKakaoDiscoveryOptions(permissionOption);

        if (!_isKakaoEditMode) return;

        var quoteDatas = _kakaoPost.content_decorators is { Count: > 0 } ? _kakaoPost.content_decorators : KakaoStoryUtils.GetQuoteDataFromString(_kakaoPost.content ?? string.Empty);
        KakaoEditorContents = [.. KakaoStoryUtils.ConvertToBaseContents(quoteDatas).Where(content => content is not MediaContent)];
        IsKakaoCommentWritable = _kakaoPost.comment_all_writable;
        foreach (var medium in _kakaoPost.media ?? []) MediaAttachments.Add(MediaAttachmentViewModel.CreateFromKakaoServer(this, medium));
        if (_kakaoPost.scrap != null) ExternalUrlContent = KakaoStoryUtils.CreateExternalUrlContent(_kakaoPost.scrap);
    }

    // Kakao Story-only write entry point of the composer: the composed post is written to
    // Kakao Story and mirrored to History afterwards.
    public ComposePostWindowViewModel(ApplicationSettings settings, bool isKakaoWriteMode) : this(settings)
    {
        _isKakaoWriteMode = isKakaoWriteMode;

        // A new post starts from the last used scope narrowed to the Kakao Story permissions.
        var lastUsedOption = CommonShared.LastUsedPostDiscoveryOption is DiscoveryOption.Everyone or DiscoveryOption.OnlyMe ? CommonShared.LastUsedPostDiscoveryOption : DiscoveryOption.Friends;
        InitializeKakaoDiscoveryOptions(lastUsedOption);
    }

    // Kakao Story only supports the three permission values.
    private void InitializeKakaoDiscoveryOptions(DiscoveryOption permissionOption)
    {
        foreach (var item in DiscoveryOptionItems.Where(x => x.Option is not (DiscoveryOption.OnlyMe or DiscoveryOption.Friends or DiscoveryOption.Everyone)).ToList()) DiscoveryOptionItems.Remove(item);
        SelectedDiscoveryOptionItem = DiscoveryOptionItems.FirstOrDefault(x => x.Option == permissionOption) ?? DiscoveryOptionItems[0];
    }

    // Kakao cross-post toggle; the saved session is validated before the mirror upload.
    [ObservableProperty]
    public partial bool IsKakaoPostEnabled { get; set; }

    // Persists the toggle through the application settings so the next compose
    // window restores the last used state.
    partial void OnIsKakaoPostEnabledChanged(bool value)
    {
        if (_isSuppressingKakaoPostEnabledPersist) return;
        _settings.IsKakaoPostEnabled = value;
    }

    // Restores the last used toggle state on window load. While the feature set is locked the
    // toggle is forced off without overwriting the saved state.
    public void LoadIsKakaoPostEnabledSetting()
    {
        if (!KakaoStoryFeatureGateHelper.IsEnabled)
        {
            _isSuppressingKakaoPostEnabledPersist = true;
            IsKakaoPostEnabled = false;
            _isSuppressingKakaoPostEnabledPersist = false;
            return;
        }

        IsKakaoPostEnabled = _settings.IsKakaoPostEnabled;
    }

    // Checks the text that is about to be uploaded to Kakao Story for profanity. When matches are
    // found the user can rewrite the post in the review dialog (the returned contents), keep the
    // original text for the upload, or cancel the upload (null). The setting lives in the shared
    // configuration so the same toggle drives every Kakao Story upload.
    private async Task<List<BaseContent>> TryResolveKakaoStoryProfanityAsync(List<BaseContent> contents)
    {
        if (Configuration.GetValue<bool?>(KakaoStoryProfanityCheckEnabledKey) is false) return contents;

        await ProfanityFilterHelper.LoadAsync();
        var profanityWords = ProfanityFilterHelper.FindProfanity(GetKakaoStoryText(contents));
        if (profanityWords.Count == 0) return contents;

        var profanityWordList = string.Join(", ", profanityWords.Take(20));
        if (profanityWords.Count > 20) profanityWordList += $" 외 {profanityWords.Count - 20}개";

        var choice = await ShowMessageDialogAsync(new MessageDialogParameters("욕설 감지", $"카카오스토리에 게시할 글에서 다음 욕설이 감지되었습니다:\n\n{profanityWordList}\n\n자동화된 계정 정지를 방지하기 위해 글 내용을 수정하시겠습니까?", "글 수정", cancelButtonText: "그대로 게시"));
        if (choice != ContentDialogResult.Primary) return contents;

        var rewriteDialog = new KakaoStoryRewriteDialog(this, profanityWordList, contents);
        var rewriteResult = await ShowContentDialogAsync(rewriteDialog);
        return rewriteResult == ContentDialogResult.Primary ? rewriteDialog.EditedContents : null;
    }

    // Blocks the submit when the Kakao Story text limit would be exceeded so the
    // mirror never fails after the History write.
    private async Task<bool> TryValidateKakaoStoryMirrorAsync(List<BaseContent> contents)
    {
        var textLength = GetKakaoStoryText(contents).Length;
        if (textLength <= 4000) return true;

        await ShowMessageDialogAsync(new MessageDialogParameters(Constants.ErrorTitle, $"카카오스토리의 글자 수 제한은 4,000자입니다. 현재 작성하신 게시글은 {textLength}자로 제한을 초과하여 카카오스토리에 게시할 수 없습니다. 게시글 내용을 수정하신 후 다시 시도해 주세요."));
        return false;
    }

    // Posts the given contents to Kakao Story with the composer's current media,
    // audience, and share setting. Images skipped by the mirror are reported after
    // the attempt so the user knows the Kakao Story post is missing some of them.
    private async Task<KakaoStoryMirrorResult> TryWriteKakaoStoryPostAsync(List<BaseContent> contents)
    {
        ShowLoading("카카오스토리에 게시하는 중...");
        try
        {
            var mirrorResult = await KakaoStoryUtils.WriteMirrorPostAsync(contents, MediaAttachments, ExternalUrlContent, SelectedDiscoveryOption, IsShareRepostDisallowed);
            if (mirrorResult.IsSuccess && mirrorResult.SkippedImageCount > 0) await ShowMessageDialogAsync(new MessageDialogParameters("카카오 게시", $"{mirrorResult.SkippedImageCount}개의 이미지가 카카오스토리 게시글에서 제외되었습니다. (형식 변환 실패 또는 이미지 수 제한 20개 초과)"));
            return mirrorResult;
        }
        finally { HideLoading(); }
    }

    // Detects a fortune-only post, i.e. a single #오늘의운세 hashtag optionally
    // accompanied by blank text. The server replaces its contents with the generated
    // fortune message, so the Kakao Story mirror must use the write response.
    private static bool IsFortuneOnlyPost(List<BaseContent> contents)
    {
        var fortuneHashtag = contents.OfType<HashtagContent>().FirstOrDefault(x => x.Tag == "오늘의운세");
        return fortuneHashtag != null && contents.All(x => x is HashtagContent || (x is TextContent blankText && string.IsNullOrWhiteSpace(blankText.Text)));
    }

    // Builds a plain-text representation from the write response contents, used for
    // the after-the-fact Kakao Story mirroring of fortune posts.
    private static string BuildKakaoStoryTextFromPostContents(List<BaseContent> contents)
    {
        var textBuilder = new StringBuilder();
        foreach (var content in contents)
        {
            if (content is TextContent text) textBuilder.Append(text.Text);
            else if (content is HashtagContent hashtag) textBuilder.Append('#').Append(hashtag.Tag).Append(' ');
        }
        return textBuilder.ToString().Trim();
    }

    // Builds a plain-text representation from the editor contents, used for the
    // Kakao Story 4,000-character limit check.
    private static string GetKakaoStoryText(List<BaseContent> contents)
    {
        var textBuilder = new StringBuilder();
        foreach (var content in contents)
        {
            if (content is TextContent text) textBuilder.Append(text.Text);
            else if (content is HashtagContent hashtag) textBuilder.Append('#').Append(hashtag.Tag).Append(' ');
            else if (content is ProfileContent profile) textBuilder.Append('@').Append(profile.Nickname).Append(' ');
            else if (content is HyperlinkContent hyperlink) textBuilder.Append(hyperlink.Url).Append(' ');
        }
        return textBuilder.ToString().Trim();
    }

    // Kakao Story edit/share submit: validates the content and the 4,000-character limit,
    // runs the Kakao Story write, then refreshes the open surfaces and closes the composer.
    private async Task SubmitKakaoStoryAsync(string plainText, List<BaseContent> editorContents)
    {
        if (!_isKakaoShareMode && string.IsNullOrWhiteSpace(plainText) && MediaAttachments.Count == 0 && ExternalUrlContent == null && !editorContents.OfType<HashtagContent>().Any() && !editorContents.OfType<StickerContent>().Any())
        {
            await ShowMessageDialogAsync(new MessageDialogParameters(Constants.ErrorTitle, "빈 내용의 글은 작성할 수 없습니다"));
            return;
        }

        var textLength = GetKakaoStoryText(editorContents).Length;
        if (textLength > 4000)
        {
            await ShowMessageDialogAsync(new MessageDialogParameters(Constants.ErrorTitle, $"카카오스토리의 글자 수 제한은 4,000자입니다. 현재 작성하신 글은 {textLength}자로 제한을 초과합니다. 글 내용을 수정하신 후 다시 시도해 주세요."));
            return;
        }

        // Only the Kakao Story upload receives the rewritten text; the History mirror keeps the
        // post as composed.
        var kakaoContents = await TryResolveKakaoStoryProfanityAsync(editorContents);
        if (kakaoContents == null) return;

        if (!await KakaoStoryUtils.EnsureLoggedInAsync(this))
        {
            await ShowMessageDialogAsync(new MessageDialogParameters(Constants.ErrorTitle, "카카오스토리 로그인에 실패하였습니다."));
            return;
        }

        IsUploading = true;
        ShowLoading(_isKakaoWriteMode ? "카카오스토리에 게시하는 중..." : _isKakaoEditMode ? "카카오스토리 게시글 수정 중..." : "카카오스토리 게시글 공유 중...");
        try
        {
            if (_isKakaoWriteMode && !await TryWriteKakaoStoryWriteAsync(editorContents, kakaoContents)) return;
            if (_isKakaoEditMode) await ExecuteKakaoStoryEditAsync(kakaoContents);
            else if (_isKakaoShareMode) await ExecuteKakaoStoryShareAsync(kakaoContents);

            foreach (var attachment in MediaAttachments) attachment.Dispose();
            if (IsTimelineRefreshEnabled) WeakReferenceMessenger.Default.Send(new RefreshButtonClickedMessage());
            SubmitCompleted?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception exception) { await ShowMessageDialogAsync(new MessageDialogParameters(Constants.ErrorTitle, $"카카오스토리 API 오류가 발생하였습니다: {KakaoStoryUtils.GetApiErrorMessage(exception)}")); }
        finally
        {
            IsUploading = false;
            HideLoading();
        }
    }

    // Writes the edited post back: the kept server media stay in place, new attachments
    // upload, and the scrap card is kept (or appended as text when media is attached).
    private async Task ExecuteKakaoStoryEditAsync(List<BaseContent> editorContents)
    {
        var mediaData = await BuildKakaoStoryEditMediaDataAsync(editorContents);
        var scrap = await BuildKakaoStoryEditScrapAsync(editorContents, mediaData);
        var quoteDatas = KakaoStoryUtils.GetQuoteDataFromContents(editorContents);
        var permission = KakaoStoryUtils.MapDiscoveryOptionToKakaoPermission(SelectedDiscoveryOption);
        var editOldMediaPaths = (_kakaoPost.media ?? []).Select(media => media.media_path).Where(mediaPath => mediaPath != null).ToList();
        await KakaoStoryApiHandler.WritePost(quoteDatas, mediaData, permission, IsKakaoCommentWritable, _kakaoPost.sharable, null, null, scrap, true, editOldMediaPaths, _kakaoPost.id);
    }

    // Shares the origin post with the composed text and the selected permission.
    private async Task ExecuteKakaoStoryShareAsync(List<BaseContent> editorContents)
    {
        var quoteDatas = KakaoStoryUtils.GetQuoteDataFromContents(editorContents);
        var permission = KakaoStoryUtils.MapDiscoveryOptionToKakaoPermission(SelectedDiscoveryOption);
        await KakaoStoryApiHandler.SharePost(_kakaoPost.id, quoteDatas, permission, true, null, null);
    }

    // Builds the media payload for the edited post. Editor stickers upload as images ahead
    // of the photos; kept server media carry their media path without re-uploading.
    private async Task<MediaData> BuildKakaoStoryEditMediaDataAsync(List<BaseContent> editorContents)
    {
        var medias = new List<MediaData.MediaObject>();

        foreach (var stickerContent in editorContents.OfType<StickerContent>())
        {
            if (stickerContent.StickerMediaId == null) continue;
            var stickerMedia = await KakaoStoryUtils.TryUploadStickerMediaAsync(stickerContent.StickerMediaId);
            if (stickerMedia == null) continue;
            medias.Add(stickerMedia);
        }

        foreach (var attachment in MediaAttachments)
        {
            if (attachment.IsKakaoServerMedia)
            {
                medias.Add(new MediaData.MediaObject { media_path = attachment.KakaoServerPath, media_type = attachment.IsVideo ? "video" : "image", caption = KakaoStoryUtils.BuildKakaoStoryMediaCaption(attachment.Description) });
                continue;
            }

            if (attachment.IsVideo) medias.Add(await KakaoStoryUtils.UploadVideoAttachmentAsync(attachment));
            else
            {
                var imageMedia = await KakaoStoryUtils.TryUploadImageAttachmentAsync(attachment);
                if (imageMedia != null) medias.Add(imageMedia);
            }
        }

        if (medias.Count == 0) return null;

        var hasImage = medias.Any(media => media.media_type == "image");
        var hasVideo = medias.Any(media => media.media_type == "video");
        return new MediaData { media = medias, media_type = hasImage && hasVideo ? "mixed" : hasImage ? "image" : "video" };
    }

    // Resolves the external URL for the edited post. Kakao Story cannot attach a scrap card
    // together with media, so the URL is appended to the body as plain text in that case.
    private async Task<string> BuildKakaoStoryEditScrapAsync(List<BaseContent> editorContents, MediaData mediaData)
    {
        var sourceUrl = ExternalUrlContent?.SourceUrl;
        if (string.IsNullOrWhiteSpace(sourceUrl)) return null;

        if (mediaData != null)
        {
            var lastTextContent = editorContents.OfType<TextContent>().LastOrDefault();
            if (lastTextContent != null && !string.IsNullOrWhiteSpace(lastTextContent.Text)) lastTextContent.Text += $"\n\n{sourceUrl}";
            else editorContents.Add(new TextContent { Text = sourceUrl });
            return null;
        }

        try { return await KakaoStoryUtils.GetScrapDataWithRetryAsync(sourceUrl); }
        catch { return null; }
    }

    // Writes a new Kakao Story post and mirrors it to History. The Kakao Story write runs
    // first so a failure leaves nothing published and the composer stays open; the History
    // mirror reports its own failure because the Kakao Story post is already published.
    private async Task<bool> TryWriteKakaoStoryWriteAsync(List<BaseContent> historyContents, List<BaseContent> kakaoContents)
    {
        var mirrorResult = await TryWriteKakaoStoryPostAsync(kakaoContents);
        if (mirrorResult.IsSuccess)
        {
            ShowLoading("히스토리에 게시하는 중...");
            await TryWriteHistoryMirrorAsync(BuildHistoryMirroredContents(historyContents));
            return true;
        }

        await ShowMessageDialogAsync(new MessageDialogParameters(Constants.ErrorTitle, $"카카오스토리 게시에 실패했습니다: {mirrorResult.ErrorMessage}"));
        return false;
    }

    // Resolves Kakao Story mentions to History friends by nickname so the mirrored History
    // post links the same people; unmatched mentions and stickers degrade to plain text
    // because History cannot reference them.
    private static List<BaseContent> BuildHistoryMirroredContents(List<BaseContent> contents)
    {
        var mirroredContents = new List<BaseContent>();
        var nicknameCache = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var content in contents)
        {
            if (content is ProfileContent profileContent)
            {
                if (!nicknameCache.TryGetValue(profileContent.Nickname, out var matchedUserId))
                {
                    matchedUserId = CommonShared.Friends?.FirstOrDefault(x => x.Nickname == profileContent.Nickname)?.UserId;
                    nicknameCache[profileContent.Nickname] = matchedUserId;
                }

                if (matchedUserId != null) mirroredContents.Add(new ProfileContent { UserId = matchedUserId, Nickname = profileContent.Nickname });
                else mirroredContents.Add(new TextContent { Text = "@" + profileContent.Nickname });
            }
            else if (content is StickerContent) mirroredContents.Add(new TextContent { Text = "(스티커)" });
            else mirroredContents.Add(content);
        }
        return mirroredContents;
    }

    // Mirrors the completed Kakao Story post to History with the same media files. The
    // Kakao Story post is the source of truth, so a mirror failure only informs the user.
    private async Task TryWriteHistoryMirrorAsync(List<BaseContent> mirroredContents)
    {
        var files = new Dictionary<string, byte[]>();
        var mediaAndUploadContents = new List<BaseContent>();
        foreach (var attachment in MediaAttachments)
        {
            mediaAndUploadContents.Add(new UploadContent { Description = string.IsNullOrEmpty(attachment.Description) ? null : attachment.Description, FileName = attachment.FileName, IsSpoiler = attachment.IsSpoiler });
            files.Add(attachment.FileName, attachment.Data);
        }

        var contents = mirroredContents.Concat(mediaAndUploadContents).ToList();
        if (ExternalUrlContent != null) contents.Add(ExternalUrlContent);

        var result = await ExecuteRequestAsync(new WritePost(contents, SelectedDiscoveryOption, null, false, files: files), ErrorType.BadRequest);
        if (!result.IsSuccess) await ShowMessageDialogAsync(new MessageDialogParameters("안내", $"카카오스토리에는 게시되었지만 히스토리 게시에 실패하였습니다: {result.ErrorMessage}"));
    }
}