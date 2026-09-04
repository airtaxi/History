using System.ComponentModel;
using History.Commons.Api.Sticker;
using History.Commons.DataTypes.Contents;
using History.WindowsClient.Dialogs;
using History.WindowsClient.Helpers;
using History.WindowsClient.Messages;
using History.WindowsClient.Models;
using History.WindowsClient.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.Windows.Storage.Pickers;
using Windows.Graphics;
using WinUIEx;

namespace History.WindowsClient.Views;

// Comment edit window hosting the EditCommentWindowViewModel. Subclasses BaseWindow
// so the theme, icon, centering, and loading-message routing apply automatically; the
// view model's dialog/picker/loading events are fulfilled directly on this window's content.
public sealed partial class EditCommentWindow : BaseWindow
{
    private readonly EditCommentWindowViewModel _viewModel;

    public EditCommentWindowViewModel ViewModel => _viewModel;

    public EditCommentWindow(EditCommentWindowViewModel viewModel) : base()
    {
        _viewModel = viewModel;

        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        this.CenterOnScreen();

        SubscribeViewModelEvents();
    }

    // no-op for this window
    protected override void Navigate(Type pageType, object parameter) { }

    // no-op for this window
    protected override bool TryNavigateBack() => false;

    protected override void ShowLoading(string message = null)
    {
        if (DispatcherQueue.HasThreadAccess) SetLoadingState(Visibility.Visible, message);
        else DispatcherQueue.TryEnqueue(() => SetLoadingState(Visibility.Visible, message));
    }

    protected override void HideLoading()
    {
        if (DispatcherQueue.HasThreadAccess) SetLoadingState(Visibility.Collapsed, null);
        else DispatcherQueue.TryEnqueue(() => SetLoadingState(Visibility.Collapsed, null));
    }

    private void SetLoadingState(Visibility visibility, string message)
    {
        LoadingGrid.Visibility = visibility;
        AppTitleBar.IsEnabled = visibility == Visibility.Collapsed;
        CommentEditor.IsEnabled = visibility == Visibility.Collapsed;
        LoadingTextBlock.Text = message;
    }

    private void SubscribeViewModelEvents()
    {
        _viewModel.MessageDialogRequested += OnMessageDialogRequested;
        _viewModel.ContentDialogRequested += OnContentDialogRequested;
        _viewModel.FilePickRequested += OnFilePickRequested;
        _viewModel.LoadingStateRequested += OnLoadingStateRequested;
        _viewModel.CommentBox.CommentSent += OnCommentBoxCommentSent;
        _viewModel.CommentBox.StickerSelected += OnCommentBoxStickerSelected;
        _viewModel.CommentBox.PropertyChanged += OnCommentBoxPropertyChanged;
    }

    // Fits the window to the content whenever the attachment preview appears or disappears.
    private void OnCommentBoxPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(BaseCommentBoxViewModel.HasAttachment))
        {
            DispatcherQueue.TryEnqueue(UpdateWindowSize);
        }
    }

    // Fits the window to the content: measures the root grid's DesiredSize and resizes the
    // window's client area, mirroring DevWinUI ContentWindow's SizeToContent behavior. Runs
    // on load and whenever the attachment preview appears or disappears.
    private void UpdateWindowSize()
    {
        if (RootGrid.XamlRoot == null) return;

        RootGrid.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));

        var dpiScale = RootGrid.XamlRoot.RasterizationScale;
        var desiredWidth = Width;

        // Re-measure at the final width so wrap-sensitive content reports its actual height.
        RootGrid.Measure(new Windows.Foundation.Size(desiredWidth, double.PositiveInfinity));

        var desiredHeight = RootGrid.DesiredSize.Height;
        if (ExtendsContentIntoTitleBar) desiredHeight -= 30;

        AppWindow.ResizeClient(new SizeInt32((int)Math.Ceiling(desiredWidth * dpiScale), (int)Math.Ceiling(desiredHeight * dpiScale)));
        this.CenterOnScreen();
    }

    private void OnMessageDialogRequested(object sender, MessageDialogRequestedEventArgs args)
    {
        var result = Content.ShowMessageDialogAsync(args.Parameters);
        args.ResultTask = result;
    }

    // Fulfills the view model's prebuilt dialog requests (sticker picker) with the
    // window-bound dialog.
    private void OnContentDialogRequested(object sender, ContentDialogRequestedEventArgs args)
    {
        var result = Content.ShowContentDialogAsync(args.Dialog);
        args.ResultTask = result;
    }

    private void OnFilePickRequested(object sender, PickerRequestedEventArgs<FileOpenPickerParameters, PickFileResult> args)
    {
        var result = Content.PickFileAsync(args.Parameters);
        args.ResultTask = result;
    }

    // Forwards the view model's loading requests to this window's overlay through the
    // weak-reference messenger; BaseWindow routes them by XamlRoot.
    private void OnLoadingStateRequested(object sender, LoadingStateRequestedEventArgs args) => LoadingStateRequestedMessage.Send(Content.XamlRoot, args);

    // The comment box edited the comment successfully: close the window.
    private void OnCommentBoxCommentSent(object sender, EventArgs e) => Close();

    // Prefills the editor with the original comment contents and fits the window to the
    // content once the bindings are applied.
    private async void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        CommentEditor.Initialize(_viewModel);
        await CommentEditor.SetContentsAsync(_viewModel.EditorContents);
        CommentEditor.FocusEditor();

        UpdateWindowSize();

        Activate();
    }

    private void OnEscapeKeyInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        Close();
    }

    private void OnWindowClosed(object sender, WindowEventArgs args) => UnregisterMessengerRecipients();

    // Collects the editor contents and hands them to the edit comment box.
    private async void OnSaveButtonClicked(object sender, RoutedEventArgs e) => await _viewModel.CommentBox.SendCommentAsync(CommentEditor.GetContents());

    // Ctrl+Enter submits the edit (mirrors the save button flow).
    private async void OnCommentEditorSubmitRequested(object sender, EventArgs e) => await _viewModel.CommentBox.SendCommentAsync(CommentEditor.GetContents());

    // Pasted images become the replacement attachment.
    private async void OnCommentEditorImageInputRequested(object sender, string path)
    {
        var fileName = Path.GetFileName(path);
        var imageData = await File.ReadAllBytesAsync(path);
        await _viewModel.CommentBox.ApplyAttachmentAsync(fileName, imageData);
    }

    // The sticker picker returned a sticker: insert it into the editor and record its usage.
    private async void OnCommentBoxStickerSelected(object sender, StickerContent stickerContent)
    {
        var inserted = await CommentEditor.InsertStickerAsync(stickerContent);
        if (!inserted)
        {
            await _viewModel.ShowMessageDialogAsync(new MessageDialogParameters("오류", "스티커 이미지를 불러올 수 없습니다."));
            return;
        }

        _ = _viewModel.ExecuteRequestAsync(new RecordStickerUsage(stickerContent.StickerId, stickerContent.StickerContentId));
        CommentEditor.FocusEditor();
    }
}
