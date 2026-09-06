using System.ComponentModel;
using History.Commons.Api.Sticker;
using History.Commons.DataTypes.Contents;
using History.WindowsClient.Dialogs;
using History.WindowsClient.Helpers;
using History.WindowsClient.Messages;
using History.WindowsClient.Models;
using History.WindowsClient.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.Windows.Storage.Pickers;
using Windows.Graphics;
using WinUIEx;

namespace History.WindowsClient.Views;

// Compose post window hosting the ComposePostWindowViewModel. Subclasses BaseWindow so the
// theme, icon, centering, and loading-message routing apply automatically; the view model's
// dialog/picker/loading events are fulfilled directly on this window's content.
public sealed partial class ComposePostWindow : BaseWindow
{
    private static ComposePostWindow s_instance;

    public static ComposePostWindow Instance => s_instance;

    private readonly ComposePostWindowViewModel _viewModel;
    public ComposePostWindowViewModel ViewModel => _viewModel;

    public ComposePostWindow(ComposePostWindowViewModel viewModel) : base()
    {
        s_instance = this;
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
        PostEditor.IsEnabled = visibility == Visibility.Collapsed;
        LoadingTextBlock.Text = message;
    }

    private void SubscribeViewModelEvents()
    {
        _viewModel.MessageDialogRequested += OnMessageDialogRequested;
        _viewModel.InputDialogRequested += OnInputDialogRequested;
        _viewModel.ContentDialogRequested += OnContentDialogRequested;
        _viewModel.FilePickRequested += OnFilePickRequested;
        _viewModel.LoadingStateRequested += OnLoadingStateRequested;
        _viewModel.FilesPickRequested += OnFilesPickRequested;
        _viewModel.StickerSelected += OnViewModelStickerSelected;
        _viewModel.SubmitCompleted += OnSubmitCompleted;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    // Fits the window to the content: measures the root grid's DesiredSize and resizes the
    // window's client area. Runs once on load.
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

    private void OnInputDialogRequested(object sender, InputDialogRequestedEventArgs args)
    {
        var result = Content.ShowInputDialogAsync(args.Parameters);
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

    // The sticker picker returned a sticker: insert it into the editor and record its usage.
    private async void OnViewModelStickerSelected(object sender, StickerContent stickerContent)
    {
        var inserted = await PostEditor.InsertStickerAsync(stickerContent);
        if (!inserted)
        {
            await _viewModel.ShowMessageDialogAsync(new MessageDialogParameters("오류", "스티커 이미지를 불러올 수 없습니다."));
            return;
        }

        _ = _viewModel.ExecuteRequestAsync(new RecordStickerUsage(stickerContent.StickerId, stickerContent.StickerContentId));
        PostEditor.FocusEditor();
    }

    private void OnFilesPickRequested(object sender, PickerRequestedEventArgs<FileOpenPickerParameters, IReadOnlyList<PickFileResult>> args)
    {
        var result = Content.PickFilesAsync(args.Parameters);
        args.ResultTask = result;
    }

    // An image was pasted into the editor: add it to the attachment list.
    private async void OnPostEditorImageInputRequested(object sender, string path) => await _viewModel.AddImageAttachmentAsync(path);

    // Fits the window to the content whenever the attachment strip or the URL preview
    // appears or disappears.
    private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ComposePostWindowViewModel.MediaAttachmentsVisibility) or nameof(ComposePostWindowViewModel.ExternalUrlContentVisibility) or nameof(ComposePostWindowViewModel.PollContentVisibility))
        {
            DispatcherQueue.TryEnqueue(UpdateWindowSize);
        }
    }

    private async void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        PostEditor.Initialize(_viewModel);
        if (_viewModel.IsShareMode)
        {
            Title = "게시글 공유";
            AppTitleBar.Title = "게시글 공유";
            SubmitButton.Content = "공유";
            CaptionTextBlock.Text = "원하는 친구와 나누고 싶은 이야기를 적어보세요.";
            PostEditor.PlaceholderText = "공유할 내용을 입력하세요";
        }
        else if (_viewModel.IsEditMode)
        {
            await PostEditor.SetContentsAsync(_viewModel.Post.Contents);
            Title = "게시글 수정";
            AppTitleBar.Title = "게시글 수정";
            SubmitButton.Content = "수정";
            ReservationButton.Visibility = Visibility.Collapsed;
        }

        PostEditor.FocusEditor();

        UpdateWindowSize();

        Activate();
    }

    private void OnEscapeKeyInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        Close();
    }

    // The view model finished the write successfully: close the composer.
    private void OnSubmitCompleted(object sender, EventArgs e) => Close();

    // Collects the editor contents and hands them to the submit flow.
    private async void OnSubmitButtonClicked(object sender, RoutedEventArgs e) => await _viewModel.SubmitAsync(PostEditor.Text, PostEditor.GetContents());

    private void OnWindowClosed(object sender, WindowEventArgs args) => UnregisterMessengerRecipients();

    // Rebuilds the comment permission menu so the check mark follows the current selection
    // and out-of-scope permissions are disabled and cannot be picked.
    private void OnCommentPermissionMenuFlyoutOpening(object sender, object e)
    {
        CommentPermissionMenuFlyout.Items.Clear();
        foreach (var item in _viewModel.CommentPermissionItems)
        {
            var menuItem = new ToggleMenuFlyoutItem
            {
                Text = item.DisplayText,
                Icon = new FontIcon { Glyph = item.Glyph, FontSize = 16 },
                IsChecked = ReferenceEquals(item, _viewModel.SelectedCommentPermissionItem),
                IsEnabled = item.IsEnabled,
                Tag = item
            };
            menuItem.Click += OnCommentPermissionMenuFlyoutItemClicked;
            CommentPermissionMenuFlyout.Items.Add(menuItem);
        }
    }

    private void OnCommentPermissionMenuFlyoutItemClicked(object sender, RoutedEventArgs e)
    {
        if ((sender as ToggleMenuFlyoutItem)?.Tag is not ComposePostCommentPermissionItemViewModel item) return;
        _viewModel.SelectedCommentPermissionItem = item;
    }

    // Ctrl+Enter submits the post (mirrors the submit button flow).
    private async void OnPostEditorSubmitRequested(object sender, EventArgs e) => await _viewModel.SubmitAsync(PostEditor.Text, PostEditor.GetContents());
}
