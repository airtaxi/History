using History.Commons.Api.Sticker;
using History.Commons.DataTypes.Contents;
using History.Commons.DataTypes.ResponseDtos;
using History.WindowsClient.Models;
using History.WindowsClient.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace History.WindowsClient.Pages;

public sealed partial class PostPage : BasePage
{
    protected override HistoryPostPageViewModel ViewModel { get; }

    public PostPage()
    {
        ViewModel = App.Services.GetRequiredService<HistoryPostPageViewModel>();

        InitializeComponent();

        CommentEditor.Initialize(ViewModel);
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        if (e.Parameter is PostResponseDto historyData) ViewModel.Initialize(historyData);
        if (ViewModel.CommentBox != null)
        {
            ViewModel.CommentBox.CommentSent -= OnCommentBoxCommentSent;
            ViewModel.CommentBox.CommentSent += OnCommentBoxCommentSent;
            ViewModel.CommentBox.StickerSelected -= OnCommentBoxStickerSelected;
            ViewModel.CommentBox.StickerSelected += OnCommentBoxStickerSelected;
        }

        base.OnNavigatedTo(e);

        // Keep the comment column anchored at the newest comment after layout settles.
        ScrollCommentsToEnd();
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);

        if (ViewModel.CommentBox != null)
        {
            ViewModel.CommentBox.CommentSent -= OnCommentBoxCommentSent;
            ViewModel.CommentBox.StickerSelected -= OnCommentBoxStickerSelected;
        }
    }

    // Pasted images become the comment attachment (mirrors the MAUI clipboard paste flow).
    private async void OnCommentEditorImageInputRequested(object sender, string path)
    {
        var fileName = Path.GetFileName(path);
        var imageData = await File.ReadAllBytesAsync(path);
        await ViewModel.CommentBox.ApplyAttachmentAsync(fileName, imageData);
    }

    // Ctrl+Enter submits the comment (mirrors the send button flow).
    private async void OnCommentEditorSubmitRequested(object sender, EventArgs e) => await ViewModel.CommentBox.SendCommentAsync(CommentEditor.GetContents());

    // Collects the editor contents and hands them to the platform comment box.
    private async void OnSendCommentButtonClicked(object sender, RoutedEventArgs e) => await ViewModel.CommentBox.SendCommentAsync(CommentEditor.GetContents());

    // The comment box sent a comment successfully: reset the composer and anchor the comment
    // column at the newest comment.
    private void OnCommentBoxCommentSent(object sender, EventArgs e)
    {
        CommentEditor.Clear();
        CommentEditor.FocusEditor();
        ScrollCommentsToEnd();
    }

    // The sticker picker returned a sticker: insert it into the comment editor and record the
    // usage (mirrors the MAUI sticker attach flow).
    private async void OnCommentBoxStickerSelected(object sender, StickerContent stickerContent)
    {
        var inserted = await CommentEditor.InsertStickerAsync(stickerContent);
        if (!inserted)
        {
            await ViewModel.ShowMessageDialogAsync(new MessageDialogParameters("오류", "스티커 이미지를 불러올 수 없습니다."));
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