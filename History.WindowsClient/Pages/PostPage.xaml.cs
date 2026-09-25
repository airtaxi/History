using CommunityToolkit.Mvvm.Messaging;
using History.Commons;
using History.Commons.Api.Sticker;
using History.Commons.Api.User;
using History.Commons.DataTypes.Contents;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using History.WindowsClient.Messages;
using History.WindowsClient.Models;
using History.WindowsClient.Services;
using History.WindowsClient.ViewModels.Post;
using History.WindowsClient.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace History.WindowsClient.Pages;

// Post detail page: dispatches between the History post and the Kakao Story post; each
// platform page view model wires its own comment box.
public sealed partial class PostPage : BasePage, IRecipient<RefreshRequestedMessage>, IRecipient<CommentReplyRequestedMessage>, IRecipient<NotificationPostRefreshRequestedMessage>
{
    private readonly HistoryPostPageViewModel _historyViewModel;
    private readonly NotificationPostDisplayService _notificationPostDisplayService;

    protected override BasePostPageViewModel ViewModel => _activeViewModel;

    private BasePostPageViewModel _activeViewModel;

    public PostPage()
    {
        _historyViewModel = App.Services.GetRequiredService<HistoryPostPageViewModel>();
        _notificationPostDisplayService = App.Services.GetRequiredService<NotificationPostDisplayService>();

        InitializeComponent();

        CommentEditor.Initialize(_historyViewModel);

        WeakReferenceMessenger.Default.Register((IRecipient<RefreshRequestedMessage>)this);
        WeakReferenceMessenger.Default.Register((IRecipient<CommentReplyRequestedMessage>)this);
        WeakReferenceMessenger.Default.Register((IRecipient<NotificationPostRefreshRequestedMessage>)this);
    }

    public void Receive(RefreshRequestedMessage message)
    {
        if (message.XamlRoot != XamlRoot) return;
        if (IsInForeground && ViewModel.Post != null)
        {
            _ = ViewModel.Post.RefreshAsync();
        }
    }

    // The background notification service saw activity for a post; the open post reloads when it
    // is the same post, regardless of whether the window itself is in the foreground. Reloading
    // also clears the notifications that point at the post so the unread badge stays current.
    public void Receive(NotificationPostRefreshRequestedMessage message)
    {
        if (!IsInForeground) return;
        if (ViewModel.Post == null) return;
        if (GetActivePlatform() != message.Platform) return;
        if (ViewModel.Post.PostId != message.PostId) return;

        _ = ViewModel.Post.RefreshAsync();
        _ = MarkPostNotificationsAsReadAsync();
    }

    // A comment reply was requested: append the comment author mention to the editor and focus it.
    public void Receive(CommentReplyRequestedMessage message)
    {
        if (!IsInForeground) return;
        if (ViewModel.CommentBox == null) return;

        CommentEditor.AppendMention(message.Value);
        CommentEditor.FocusEditor();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        if (e.Parameter is PostResponseDto historyData)
        {
            _historyViewModel.Initialize(historyData);
            _activeViewModel = _historyViewModel;
        }
        else if (!TryInitializeKakaoStory(e.Parameter)) _activeViewModel = _historyViewModel;

        // Kakao Story posts mention Kakao Story friends; History posts keep History friends.
        CommentEditor.IsKakaoMentionMode = ViewModel.Post is KakaoPostViewModel;

        base.OnNavigatedTo(e);

        // The open post is published so the notification service can suppress a toast for it and
        // ask for a reload while it stays open.
        if (ViewModel.Post == null) _notificationPostDisplayService.ClearDisplayedPost();
        else _notificationPostDisplayService.SetDisplayedPost(GetActivePlatform(), ViewModel.Post.PostId);

        // Kakao Story has no server read endpoint, so viewing the post is what clears its toasts.
        if (ViewModel.Post != null && GetActivePlatform() == NotificationPostPlatform.KakaoStory) _notificationPostDisplayService.MarkPostRead(NotificationPostPlatform.KakaoStory, ViewModel.Post.PostId);

        _ = MarkPostNotificationsAsReadAsync();

        // Keep the comment column anchored at the newest comment after layout settles.
        ScrollCommentsToEnd();
    }

    // Leaving the post clears the published display state so the notification service stops
    // treating this post as the one the user is looking at.
    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        _notificationPostDisplayService.ClearDisplayedPost();
        base.OnNavigatedFrom(e);
    }

    // The platform of the post currently shown by this page.
    private NotificationPostPlatform GetActivePlatform() => ViewModel.Post is KakaoPostViewModel ? NotificationPostPlatform.KakaoStory : NotificationPostPlatform.History;

    // The comment box belongs to the active platform view model, so its events follow the
    // active view model through the navigation lifecycle.
    protected override void SubscribeViewModelEvents()
    {
        base.SubscribeViewModelEvents();

        if (ViewModel.CommentBox == null) return;

        ViewModel.CommentBox.CommentSent += OnCommentBoxCommentSent;
        ViewModel.CommentBox.StickerSelected += OnCommentBoxStickerSelected;
    }

    protected override void UnsubscribeViewModelEvents()
    {
        if (ViewModel.CommentBox != null)
        {
            ViewModel.CommentBox.CommentSent -= OnCommentBoxCommentSent;
            ViewModel.CommentBox.StickerSelected -= OnCommentBoxStickerSelected;
        }

        base.UnsubscribeViewModelEvents();
    }

    // Marks notifications that point to this post as read and broadcasts the result so
    // unread badges and notification list entries update.
    private async Task MarkPostNotificationsAsReadAsync()
    {
        if (ViewModel.Post is not HistoryPostViewModel historyPostViewModel) return;

        var postId = historyPostViewModel.Post.Id;
        var success = await CommonShared.ApiHandler.TryExecuteRequestAsync(new ReadNotificationsByPostId(postId));
        if (success)
        {
            WeakReferenceMessenger.Default.Send(new NotificationPostReadMessage(postId));

            // Clearing the read notifications also dismisses the matching system toasts.
            _notificationPostDisplayService.MarkPostRead(NotificationPostPlatform.History, postId);
        }
    }

    // Pasted images become the comment attachment.
    private async void OnCommentEditorImageInputRequested(object sender, string path)
    {
        if (ViewModel.CommentBox == null) return;

        var fileName = Path.GetFileName(path);
        var imageData = await File.ReadAllBytesAsync(path);
        await ViewModel.CommentBox.ApplyAttachmentAsync(fileName, imageData);
    }

    // Ctrl+Enter submits the comment (mirrors the send button flow).
    private async void OnCommentEditorSubmitRequested(object sender, EventArgs e)
    {
        if (ViewModel.CommentBox == null) return;

        await ViewModel.CommentBox.SendCommentAsync(CommentEditor.GetContents());
    }

    // Collects the editor contents and hands them to the platform comment box.
    private async void OnSendCommentButtonClicked(object sender, RoutedEventArgs e)
    {
        if (ViewModel.CommentBox == null) return;

        await ViewModel.CommentBox.SendCommentAsync(CommentEditor.GetContents());
    }

    // The comment box sent a comment successfully: reset the composer and anchor the comment
    // column at the newest comment.
    private void OnCommentBoxCommentSent(object sender, EventArgs e)
    {
        CommentEditor.Clear();
        CommentEditor.FocusEditor();
        ScrollCommentsToEnd();
    }

    // The sticker picker returned a sticker: insert it into the comment editor and record its usage.
    private async void OnCommentBoxStickerSelected(object sender, StickerContent stickerContent)
    {
        var inserted = await CommentEditor.InsertStickerAsync(stickerContent);
        if (!inserted)
        {
            await ViewModel.ShowMessageDialogAsync(new MessageDialogParameters(Constants.ErrorTitle, Constants.StickerLoadErrorMessage));
            return;
        }

        _ = ViewModel.ExecuteRequestAsync(new RecordStickerUsage(stickerContent.StickerId, stickerContent.StickerContentId));
        CommentEditor.FocusEditor();
    }

    private void ScrollCommentsToEnd()
    {
        CommentScrollViewer.InvalidateMeasure();
        CommentScrollViewer.UpdateLayout();
        CommentScrollViewer.ChangeView(null, CommentScrollViewer.ScrollableHeight, null, true);
    }
}
