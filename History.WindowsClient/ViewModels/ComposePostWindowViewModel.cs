using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using History.Commons;
using History.Commons.Api.Post;
using History.Commons.DataTypes.Contents;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using History.WindowsClient.Dialogs;
using History.WindowsClient.Helpers;
using History.WindowsClient.Messages;
using History.WindowsClient.Models;
using History.WindowsClient.ViewModels.DiscoveryOptions;
using History.WindowsClient.Views;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.Storage.Pickers;

namespace History.WindowsClient.ViewModels;

// Compose post window state. Poll composing delegates to the PollEditWindow and arrives
// through the attached poll card; kakao cross-post is a stub to be filled in later;
// media attachment, the option pickers, reservation, and the submit pipeline are real.
// The same window hosts post editing (an existing post prefills the editor and submit
// runs ModifyPost) and post sharing (the origin post bounds the share's audience).
public sealed partial class ComposePostWindowViewModel : BaseViewModel
{
    private const int CommentPermissionNotSetSelectedIndex = 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedDiscoveryOptionDisplayText))]
    [NotifyPropertyChangedFor(nameof(SelectedDiscoveryOptionGlyph))]
    [NotifyPropertyChangedFor(nameof(SelectedDiscoveryIndex))]
    public partial ComposePostDiscoveryOptionItemViewModel SelectedDiscoveryOptionItem { get; set; }

    public DiscoveryOption SelectedDiscoveryOption => SelectedDiscoveryOptionItem?.Option ?? DiscoveryOption.FriendsOfFriends;

    [ObservableProperty]
    public partial bool IsShareRepostDisallowed { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedCommentPermissionDisplayText))]
    [NotifyPropertyChangedFor(nameof(SelectedCommentPermissionGlyph))]
    public partial ComposePostCommentPermissionItemViewModel SelectedCommentPermissionItem { get; set; }

    // Reservation state: the flyout toggle arms scheduling, and the date/time pickers are
    // only editable while it is on. Both values must be present for the reservation to
    // count; turning the toggle off clears them.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReservationButtonContent))]
    [NotifyPropertyChangedFor(nameof(ReservationToolTip))]
    [NotifyPropertyChangedFor(nameof(ReservationDateTime))]
    public partial bool IsReservationEnabled { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReservationButtonContent))]
    [NotifyPropertyChangedFor(nameof(ReservationToolTip))]
    [NotifyPropertyChangedFor(nameof(ReservationDateTime))]
    public partial DateTimeOffset? ReservationDate { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReservationButtonContent))]
    [NotifyPropertyChangedFor(nameof(ReservationToolTip))]
    [NotifyPropertyChangedFor(nameof(ReservationDateTime))]
    public partial TimeSpan? ReservationTime { get; set; }

    // Kakao cross-post toggle. The login flow is not wired up yet; the toggle itself only
    // reflects the current stub state until the kakao game posting is implemented.
    [ObservableProperty]
    public partial bool IsKakaoPostEnabled { get; set; }

    public ObservableCollection<ComposePostDiscoveryOptionItemViewModel> DiscoveryOptionItems { get; } = [.. Enum.GetValues<DiscoveryOption>().OrderBy(x => (int)x).Select(option => new ComposePostDiscoveryOptionItemViewModel(option))];

    public ObservableCollection<ComposePostCommentPermissionItemViewModel> CommentPermissionItems { get; } = [.. Enum.GetValues<AccessPermission>().OrderBy(x => (int)x).Select(permission => new ComposePostCommentPermissionItemViewModel(permission)).Prepend(new ComposePostCommentPermissionItemViewModel(null))];

    public string SelectedDiscoveryOptionDisplayText => SelectedDiscoveryOptionItem?.DisplayText ?? SelectedDiscoveryOption.ToDisplayString();

    public string SelectedDiscoveryOptionGlyph => PostHelper.GetDiscoveryOptionGlyph(SelectedDiscoveryOption);

    public int SelectedDiscoveryIndex
    {
        get => SelectedDiscoveryOptionItem == null ? 0 : DiscoveryOptionItems.IndexOf(SelectedDiscoveryOptionItem);
        set
        {
            if (value < 0 || value >= DiscoveryOptionItems.Count) return;
            SelectedDiscoveryOptionItem = DiscoveryOptionItems[value];
        }
    }

    public string SelectedCommentPermissionDisplayText => SelectedCommentPermissionItem?.DisplayText ?? CommentPermissionItems[CommentPermissionNotSetSelectedIndex].DisplayText;

    public string SelectedCommentPermissionGlyph => SelectedCommentPermissionItem?.Glyph ?? CommentPermissionItems[CommentPermissionNotSetSelectedIndex].Glyph;

    public DateTime? ReservationDateTime => IsReservationEnabled && ReservationDate is { } date && ReservationTime is { } time ? date.LocalDateTime.Date + time : null;

    public string ReservationButtonContent => ReservationDateTime is not null ? "예약됨" : "게시 예약";

    public string ReservationToolTip => ReservationDateTime is { } time ? $"게시 예약: {time:yyyy-MM-dd HH:mm}" : "게시 예약";

    // Turning the reservation off clears any partially picked date/time so an old
    // selection cannot silently come back when it is re-enabled.
    partial void OnIsReservationEnabledChanged(bool value)
    {
        if (!value)
        {
            ReservationDate = null;
            ReservationTime = null;
        }
    }

    public event EventHandler<StickerContent> StickerSelected;

    // Raised after a successful write so the window can close itself.
    public event EventHandler SubmitCompleted;

    public ObservableCollection<MediaAttachmentViewModel> MediaAttachments { get; } = [];

    public bool MediaAttachmentsVisibility => MediaAttachments.Count > 0;

    // Attached external URL card state. The preview surface is kept in sync through the
    // changed handler so the window only ever binds one view model for the card.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ExternalUrlContentVisibility))]
    public partial ExternalUrlContent ExternalUrlContent { get; set; }

    public ExternalUrlContentViewModel ExternalUrlPreview { get; } = new();

    public bool ExternalUrlContentVisibility => ExternalUrlContent != null;

    partial void OnExternalUrlContentChanged(ExternalUrlContent value) => ExternalUrlPreview.Update(value);

    // Attached poll card state. The summary surface is rebuilt from the content so the
    // window only ever binds one poll definition.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PollContentVisibility))]
    [NotifyPropertyChangedFor(nameof(PollSummaryText))]
    [NotifyPropertyChangedFor(nameof(PollToolTip))]
    public partial PollContent PollContent { get; set; }

    public bool PollContentVisibility => PollContent != null;

    public string PollSummaryText
    {
        get
        {
            if (PollContent is not { } poll) return string.Empty;

            var expirationText = poll.ExpiresAt is { } expiresAt ? $" · 종료: {expiresAt.ToLocalTime():yyyy-MM-dd HH:mm}" : " · 마감 없음";
            return $"{poll.Options.Count}개 선택지 · {(poll.AllowMultipleSelection ? "복수 선택 허용" : "단일 선택")}{expirationText}";
        }
    }

    public string PollToolTip => PollContent is not { } poll ? "투표" : $"투표: {poll.Question}";

    public PostResponseDto Post { get; }

    public PostResponseDto ParentPost { get; }

    // Edit mode reuses the composer for an existing post: the audience, permission, share
    // setting, media, and attachments are prefilled from the post and submit runs ModifyPost.
    public bool IsEditMode => Post != null;

    // Share mode composes a new post attached to an origin post; the origin's audience
    // bounds the share's discovery option.
    public bool IsShareMode => ParentPost != null;

    // The original post that bounds the audience when composing or editing a share; null
    // for ordinary posts.
    public PostResponseDto ScopeOriginPost => IsEditMode ? Post.ParentPost : ParentPost;

    public ComposePostWindowViewModel(PostResponseDto post = null, PostResponseDto parentPost = null)
    {
        Post = post;
        ParentPost = parentPost;

        if (IsShareMode)
        {
            var initialOption = (DiscoveryOption)Math.Min((int)CommonShared.LastUsedPostDiscoveryOption, (int)parentPost.DiscoveryOption);
            SelectedDiscoveryOptionItem = DiscoveryOptionItems.FirstOrDefault(x => x.Option == initialOption) ?? DiscoveryOptionItems[0];
            SelectedCommentPermissionItem = CommentPermissionItems[CommentPermissionNotSetSelectedIndex];
            return;
        }

        SelectedDiscoveryOptionItem = DiscoveryOptionItems.FirstOrDefault(x => x.Option == (post?.DiscoveryOption ?? SelectedDiscoveryOption)) ?? DiscoveryOptionItems[0];
        SelectedCommentPermissionItem = CommentPermissionItems.FirstOrDefault(x => x.Permission == post?.CommentPermission) ?? CommentPermissionItems[CommentPermissionNotSetSelectedIndex];
        if (post == null) return;

        IsShareRepostDisallowed = post.DisallowShare;
        foreach (var mediaContent in post.Contents.OfType<MediaContent>()) MediaAttachments.Add(MediaAttachmentViewModel.CreateFromServer(this, mediaContent));
        ExternalUrlContent = post.Contents.OfType<ExternalUrlContent>().FirstOrDefault();
        PollContent = post.Contents.OfType<PollContent>().FirstOrDefault();
    }

    partial void OnSelectedDiscoveryOptionItemChanged(ComposePostDiscoveryOptionItemViewModel value)
    {
        if (value == null) return;

        // A share (composing or editing) cannot widen the audience beyond the origin post's scope.
        if (ScopeOriginPost is { } originPost && value.Option > originPost.DiscoveryOption)
        {
            _ = ShowMessageDialogAsync(new MessageDialogParameters(Constants.ErrorTitle, "공유된 글의 공개 범위는 원본 글의 공개 범위보다 클 수 없습니다."));
            SelectedDiscoveryOptionItem = DiscoveryOptionItems.FirstOrDefault(x => x.Option == originPost.DiscoveryOption) ?? DiscoveryOptionItems[0];
            return;
        }

        foreach (var item in CommentPermissionItems) item.UpdateAvailability(value.Option);

        // A disabled selection cannot survive a scope narrowing: fall back to the not-set entry.
        if (SelectedCommentPermissionItem != null && !SelectedCommentPermissionItem.IsEnabled) SelectedCommentPermissionItem = CommentPermissionItems[CommentPermissionNotSetSelectedIndex];
    }

    partial void OnSelectedCommentPermissionItemChanged(ComposePostCommentPermissionItemViewModel value)
    {
        if (value == null || value.IsEnabled) return;

        // Ignore disabled picks so the selection cannot land on an unavailable permission.
        SelectedCommentPermissionItem = CommentPermissionItems[CommentPermissionNotSetSelectedIndex];
    }

    // Opens the sticker picker dialog and surfaces the chosen sticker to the window so it
    // can be inserted into the editor.
    [RelayCommand]
    private async Task HandleStickerTapAsync()
    {
        var dialog = new StickerPickerDialog(new StickerPickerViewModel(this));
        await ShowContentDialogAsync(dialog);
        if (dialog.SelectedStickerContent != null) StickerSelected?.Invoke(this, dialog.SelectedStickerContent);
    }

    // Picks one or more images and adds them to the attachment list. Files that exceed the
    // upload size limit are skipped; the remaining slots are filled in picker order.
    [RelayCommand]
    private async Task HandleMediaTapAsync()
    {
        var remainingCount = CommonConstants.MaxPostMediaCount - MediaAttachments.Count;
        if (remainingCount <= 0)
        {
            await ShowMessageDialogAsync(new MessageDialogParameters("사진/영상", $"미디어는 최대 {CommonConstants.MaxPostMediaCount}개까지 추가할 수 있습니다."));
            return;
        }

        var results = await PickFilesAsync(new FileOpenPickerParameters(Constants.MediaFileTypeFilters, PickerLocationId.PicturesLibrary, "사진/영상 추가"));
        if (results == null || results.Count == 0) return;

        if (results.Count > remainingCount) await ShowMessageDialogAsync(new MessageDialogParameters("사진/영상", $"{remainingCount}개가 넘는 미디어 파일은 무시됩니다."));

        var sizeExceededCount = 0;
        foreach (var result in results.Take(remainingCount))
        {
            if (await TryAddMediaAttachmentAsync(result.Path)) continue;
            sizeExceededCount++;
        }

        if (sizeExceededCount > 0) await ShowMessageDialogAsync(new MessageDialogParameters("사진/영상", "용량을 초과하는 미디어는 자동으로 제외되었습니다."));
    }

    // Adds an image pasted into the editor as an attachment, sharing the size checks and
    // temp-file handling of the picker flow.
    public async Task AddImageAttachmentAsync(string sourcePath)
    {
        if (MediaAttachments.Count >= CommonConstants.MaxPostMediaCount)
        {
            await ShowMessageDialogAsync(new MessageDialogParameters("사진/영상", $"미디어는 최대 {CommonConstants.MaxPostMediaCount}개까지 추가할 수 있습니다."));
            return;
        }

        await TryAddMediaAttachmentAsync(sourcePath);
    }

    // Removes the attachment from the list and deletes its temp file.
    public void RemoveAttachment(MediaAttachmentViewModel attachment)
    {
        if (!MediaAttachments.Remove(attachment)) return;
        attachment.Dispose();
        OnPropertyChanged(nameof(MediaAttachmentsVisibility));
    }

    // Copies the picked file into a uniquely named temp file and appends the attachment.
    // Images decode their bytes for the preview; videos extract a thumbnail frame and fall
    // back to the video placeholder when the frame cannot be rendered. Returns false when
    // the file exceeds its upload size limit (images 60MB, videos 100MB).
    private async Task<bool> TryAddMediaAttachmentAsync(string sourcePath)
    {
        var isVideo = IsVideoFile(sourcePath);
        var sizeLimit = isVideo ? CommonConstants.MaxUploadFileSize : CommonConstants.MaxImageUploadFileSize;
        if (new FileInfo(sourcePath).Length > sizeLimit) return false;

        var randomFileName = GenerateRandomFileName(sourcePath);
        var tempPath = Path.Combine(Path.GetTempPath(), randomFileName);
        await Task.Run(() => File.Copy(sourcePath, tempPath, true));

        var attachment = isVideo ? await MediaAttachmentViewModel.CreateVideoAsync(this, randomFileName, tempPath) : await MediaAttachmentViewModel.CreateAsync(this, randomFileName, tempPath, await File.ReadAllBytesAsync(tempPath));
        MediaAttachments.Add(attachment);
        OnPropertyChanged(nameof(MediaAttachmentsVisibility));
        return true;
    }

    private static bool IsVideoFile(string path) => Constants.VideoFileTypeFilters.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);

    private string GenerateRandomFileName(string sourcePath)
    {
        var extension = Path.GetExtension(sourcePath);
        string randomFileName;
        do randomFileName = Path.GetRandomFileName().Replace(".", string.Empty) + extension; while (MediaAttachments.Any(x => x.FileName.Equals(randomFileName, StringComparison.OrdinalIgnoreCase)));
        return randomFileName;
    }

    // Asks for a URL to attach and fills its preview card through the server. A second
    // attach replaces the existing card; removal happens through the preview's close button.
    [RelayCommand]
    private async Task HandleUrlTapAsync()
    {
        var url = await ShowInputDialogAsync(new InputDialogParameters("URL 입력", "게시글에 첨부할 URL을 입력해주세요.", "첨부하고자 하는 URL을 입력하세요", showCancel: true));
        if (string.IsNullOrWhiteSpace(url)) return;

        var fillResult = await ExecuteRequestAsync(new FillExternalUrlContent(new ExternalUrlContent { SourceUrl = url }), ErrorType.BadRequest);
        if (fillResult.IsFailure)
        {
            if (fillResult.Error == ErrorType.BadRequest) await ShowMessageDialogAsync(new MessageDialogParameters(Constants.ErrorTitle, fillResult.ErrorMessage));
            return;
        }

        ExternalUrlContent = fillResult.Value;
    }

    // Removes the attached external URL card from the composer.
    [RelayCommand]
    private void RemoveExternalUrlContent() => ExternalUrlContent = null;

    // Opens the poll edit window modally; a confirmed poll replaces the attached one. When
    // a poll is already attached, the replacement is confirmed first.
    [RelayCommand]
    private async Task HandlePollTapAsync()
    {
        // In edit mode the attached poll is opened for editing (its id is preserved); in
        // compose mode a replacement poll starts from scratch.
        var shouldReplace = PollContent != null && !IsEditMode;
        if (shouldReplace)
        {
            var replaceResult = await ShowMessageDialogAsync(new MessageDialogParameters("투표", "이미 추가된 투표가 있습니다. 새로 만들까요?", "새로 만들기", cancelButtonText: "취소"));
            if (replaceResult != ContentDialogResult.Primary) return;
        }

        var pollEditWindow = new PollEditWindow(new PollEditWindowViewModel(shouldReplace ? null : PollContent));
        pollEditWindow.ViewModel.Confirmed += OnPollEditConfirmed;
        pollEditWindow.ActivateModal(ComposePostWindow.Instance);
    }

    private void OnPollEditConfirmed(object sender, PollContent pollContent) => PollContent = pollContent;

    // Removes the attached poll card from the composer.
    [RelayCommand]
    private void RemovePoll() => PollContent = null;

    // TODO: kakao login and cross-post routing is decided here when the game posting is
    // implemented; until then the toggle press shows the login-needed hint.
    [RelayCommand]
    private async Task HandleKakaoPostTapAsync()
    {
        IsKakaoPostEnabled = false;
        await ShowMessageDialogAsync(new MessageDialogParameters("카카오 게시", "카카오 게시 연동은 아직 준비 중입니다."));
    }

    // Submits the post from the given editor snapshot: validates the reservation (write
    // only), resolves the discovery audience, assembles the media upload, and sends the
    // WritePost/ModifyPost request. A successful write saves the last-used discovery
    // option, disposes the uploaded attachments, refreshes the open feeds (or updates the
    // edited post), and raises SubmitCompleted so the window can close itself.
    public async Task SubmitAsync(string plainText, List<BaseContent> editorContents)
    {
        // Name-only posts are usually written once; prompt when the most recent post is
        // private and the current selection is private too.
        // TODO: expose an OnlyMePostContinuationPromptEnabled toggle (and a settings page
        // entry for it) so the user can turn this prompt off.
        if (!IsEditMode && !IsShareMode && SelectedDiscoveryOption == DiscoveryOption.OnlyMe && await IsMostRecentPostOnlyMeAsync())
        {
            var proceedResult = await ShowMessageDialogAsync(new MessageDialogParameters("안내", "마지막으로 작성한 게시글이 나만 보기로 설정되어 있습니다. 이 글도 나만 보기로 작성하시겠습니까?", "작성", cancelButtonText: "취소"));
            if (proceedResult != ContentDialogResult.Primary) return;
        }

        DateTime? reservationTime = null;
        if (!IsEditMode && IsReservationEnabled)
        {
            if (ReservationDate is null || ReservationTime is null)
            {
                await ShowMessageDialogAsync(new MessageDialogParameters("게시 예약", "게시 예약을 사용하려면 날짜와 시간을 모두 선택해주세요."));
                return;
            }

            reservationTime = ReservationDateTime;
            if (reservationTime <= DateTime.Now)
            {
                await ShowMessageDialogAsync(new MessageDialogParameters("게시 예약", "예약 시간은 현재보다 이후여야 합니다."));
                return;
            }
        }

        if (!IsEditMode && reservationTime != null)
        {
            var proceedResult = await ShowMessageDialogAsync(new MessageDialogParameters("게시 예약", "예약 시간을 설정하셨습니다. 예약 게시글은 예약 시간이 지나야 게시되며, 게시가 되기 전까지는 게시글을 수정할 수 없습니다. 예약 게시글을 작성하시겠습니까?", "예약", cancelButtonText: "취소"));
            if (proceedResult != ContentDialogResult.Primary) return;
        }

        var discoveryOption = SelectedDiscoveryOption;
        List<string> discoveryOptionSelectedUserIds = null;
        if (discoveryOption is DiscoveryOption.SelectedUsers or DiscoveryOption.UnselectedUsers)
        {
            discoveryOptionSelectedUserIds = await TrySelectDiscoveryOptionUsersAsync();
            if (discoveryOptionSelectedUserIds == null)
            {
                await ShowMessageDialogAsync(new MessageDialogParameters(Constants.ErrorTitle, "선택된 친구가 없습니다."));
                RevertDiscoveryOptionToLastUsed();
                return;
            }
        }

        var files = new Dictionary<string, byte[]>();
        var mediaAndUploadContents = new List<BaseContent>();
        foreach (var attachment in MediaAttachments)
        {
            // Kept server media is sent back unchanged; only new local files upload.
            if (attachment.IsServerMedia)
            {
                mediaAndUploadContents.Add(attachment.ServerContent);
                continue;
            }

            mediaAndUploadContents.Add(new UploadContent
            {
                Description = string.IsNullOrEmpty(attachment.Description) ? null : attachment.Description,
                FileName = attachment.FileName,
                IsSpoiler = attachment.IsSpoiler
            });
            files.Add(attachment.FileName, attachment.Data);
        }

        var contents = editorContents.Concat(mediaAndUploadContents).ToList();
        if (ExternalUrlContent != null) contents.Add(ExternalUrlContent);
        if (PollContent != null) contents.Add(PollContent);

        // Shares may carry no text of their own; the origin post renders as the content.
        if (!IsShareMode && string.IsNullOrWhiteSpace(plainText) && mediaAndUploadContents.Count == 0 && ExternalUrlContent == null && PollContent == null && !editorContents.OfType<HashtagContent>().Any())
        {
            await ShowMessageDialogAsync(new MessageDialogParameters(Constants.ErrorTitle, "빈 내용의 글은 작성할 수 없습니다"));
            return;
        }

        var result = IsEditMode ? await ExecuteRequestAsync(new ModifyPost(Post.Id, contents, discoveryOption, SelectedCommentPermissionItem.Permission, IsShareRepostDisallowed, discoveryOptionSelectedUserIds, files), ErrorType.BadRequest) : await ExecuteRequestAsync(new WritePost(contents, discoveryOption, SelectedCommentPermissionItem.Permission, IsShareRepostDisallowed, ParentPost?.Id, discoveryOptionSelectedUserIds, files, reservationTime?.ToUniversalTime()), ErrorType.BadRequest);
        if (result.Error == ErrorType.BadRequest)
        {
            await ShowMessageDialogAsync(new MessageDialogParameters(Constants.ErrorTitle, result.ErrorMessage));
            return;
        }
        else if (result.IsSuccess)
        {
            if (ScopeOriginPost == null) CommonShared.LastUsedPostDiscoveryOption = discoveryOption;
            foreach (var attachment in MediaAttachments) attachment.Dispose();
            if (IsEditMode) WeakReferenceMessenger.Default.Send(new ValueChangedMessage<PostResponseDto>(result.Value));
            else WeakReferenceMessenger.Default.Send(new RefreshButtonClickedMessage());
            SubmitCompleted?.Invoke(this, EventArgs.Empty);
        }
    }

    // Returns true when the user's most recent post is also written as Only Me.
    private async Task<bool> IsMostRecentPostOnlyMeAsync()
    {
        var postsResult = await ExecuteRequestAsync(new GetUserPosts(CommonShared.UserId, null, 1));
        return postsResult.IsSuccess && postsResult.Value is { Count: > 0 } && postsResult.Value[0].DiscoveryOption == DiscoveryOption.OnlyMe;
    }

    // Opens the friend picker for SelectedUsers/UnselectedUsers scopes and returns the
    // chosen user ids; null when the dialog was cancelled or nothing was selected.
    private async Task<List<string>> TrySelectDiscoveryOptionUsersAsync()
    {
        var initialUserIds = IsEditMode && Post.DiscoveryOption == SelectedDiscoveryOption ? (Post.DiscoveryOptionSelectedUserIds ?? []) : [];
        var dialog = new DiscoveryOptionSelectUsersDialog(new HistoryDiscoveryOptionSelectUsersViewModel(initialUserIds, this));
        var result = await ShowContentDialogAsync(dialog);
        if (result != ContentDialogResult.Primary) return null;
        return dialog.ViewModel.SelectedUserIds.Count > 0 ? dialog.ViewModel.SelectedUserIds : null;
    }

    // Restores the discovery combo to the last successfully used option when the friend
    // picker was cancelled or nothing was selected.
    private void RevertDiscoveryOptionToLastUsed()
    {
        var fallback = DiscoveryOptionItems.FirstOrDefault(x => x.Option == CommonShared.LastUsedPostDiscoveryOption) ?? DiscoveryOptionItems[0];
        SelectedDiscoveryOptionItem = fallback;
    }
}
