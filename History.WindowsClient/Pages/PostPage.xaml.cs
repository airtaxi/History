using CommunityToolkit.Mvvm.Messaging;
using History.Commons;
using History.Commons.Api.Sticker;
using History.Commons.Api.User;
using History.Commons.DataTypes.Contents;
using History.Commons.DataTypes.ResponseDtos;
using History.WindowsClient.Messages;
using History.WindowsClient.Models;
using History.WindowsClient.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace History.WindowsClient.Pages;

// Post detail page: dispatches between the History post and the Kakao Story post; each
// platform page view model wires its own comment box.
public sealed partial class PostPage : BasePage, IRecipient<RefreshButtonClickedMessage>, IRecipient<CommentReplyRequestedMessage>
{
    private readonly HistoryPostPageViewModel _historyViewModel;

    protected override BasePostPageViewModel ViewModel => _activeViewModel;

    private BasePostPageViewModel _activeViewModel;
    private bool _isInForeground;

    public PostPage()
    {
        _historyViewModel = App.Services.GetRequiredService<HistoryPostPageViewModel>();

        InitializeComponent();

        CommentEditor.Initialize(_historyViewModel);

        WeakReferenceMessenger.Default.Register((IRecipient<RefreshButtonClickedMessage>)this);
        WeakReferenceMessenger.Default.Register((IRecipient<CommentReplyRequestedMessage>)this);
    }

    public void Receive(RefreshButtonClickedMessage message)
    {
        if (_isInForeground && ViewModel.Post != null)
        {
            _ = ViewModel.Post.RefreshAsync();
        }
    }

    // A comment reply was requested: append the comment author mention to the editor and focus it.
    public void Receive(CommentReplyRequestedMessage message)
    {
        if (!_isInForeground) return;
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

        if (ViewModel.CommentBox != null)
        {
            ViewModel.CommentBox.CommentSent -= OnCommentBoxCommentSent;
            ViewModel.CommentBox.CommentSent += OnCommentBoxCommentSent;
            ViewModel.CommentBox.StickerSelected -= OnCommentBoxStickerSelected;
            ViewModel.CommentBox.StickerSelected += OnCommentBoxStickerSelected;
        }

        base.OnNavigatedTo(e);

        _isInForeground = true;

        _ = MarkPostNotificationsAsReadAsync();

        // Keep the comment column anchored at the newest comment after layout settles.
        ScrollCommentsToEnd();
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);

        _isInForeground = false;

        if (ViewModel.CommentBox != null)
        {
            ViewModel.CommentBox.CommentSent -= OnCommentBoxCommentSent;
            ViewModel.CommentBox.StickerSelected -= OnCommentBoxStickerSelected;
        }
    }

    // Marks notifications that point to this post as read and broadcasts the result so
    // unread badges and notification list entries update.
    private async Task MarkPostNotificationsAsReadAsync()
    {
        if (ViewModel.Post is not HistoryPostViewModel historyPostViewModel) return;

        var postId = historyPostViewModel.Post.Id;
        var success = await CommonShared.ApiHandler.TryExecuteRequestAsync(new ReadNotificationsByPostId(postId));
        if (success) WeakReferenceMessenger.Default.Send(new NotificationPostReadMessage(postId));
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