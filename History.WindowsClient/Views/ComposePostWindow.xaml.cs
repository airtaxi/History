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
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics;
using Windows.Storage;
using WinUIEx;

namespace History.WindowsClient.Views;

// Compose post window hosting the ComposePostWindowViewModel. Subclasses BaseWindow so the
// theme, icon, centering, and loading-message routing apply automatically; the view model's
// dialog/picker/loading events are fulfilled directly on this window's content.
public sealed partial class ComposePostWindow : BaseWindow
{
    private static ComposePostWindow s_instance;

    public static ComposePostWindow Instance => s_instance;

    private int WindowWidth { get; } = 520;

    private readonly ComposePostWindowViewModel _viewModel;
    public ComposePostWindowViewModel ViewModel => _viewModel;

    public ComposePostWindow(ComposePostWindowViewModel viewModel) : base(viewModel)
    {
        s_instance = this;
        _viewModel = viewModel;

        InitializeComponent();

        // The attachment strip's ListView handles drag events for its own reorder pass, so
        // the window subscribes with handledEventsToo to also receive file drags over the
        // strip and anywhere else in the window content.
        RootGrid.AddHandler(UIElement.DragOverEvent, new DragEventHandler(OnRootGridDragOver), true);
        RootGrid.AddHandler(UIElement.DropEvent, new DragEventHandler(OnRootGridDrop), true);

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        this.CenterOnScreen();
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
        // While the post media is uploading, hide requests are ignored so intermediate
        // request loadings cannot unlock the composer mid-upload.
        if (_viewModel.IsUploading) return;
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

    protected override void SubscribeViewModelEvents()
    {
        base.SubscribeViewModelEvents();

        _viewModel.StickerSelected += OnViewModelStickerSelected;
        _viewModel.SubmitCompleted += OnSubmitCompleted;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    protected override void UnsubscribeViewModelEvents()
    {
        _viewModel.StickerSelected -= OnViewModelStickerSelected;
        _viewModel.SubmitCompleted -= OnSubmitCompleted;
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;

        base.UnsubscribeViewModelEvents();
    }

    // Fits the window to the content: measures the root grid's DesiredSize and resizes the
    // window's client area. Runs once on load.
    private void UpdateWindowSize()
    {
        if (RootGrid.XamlRoot == null) return;

        RootGrid.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));

        var dpiScale = RootGrid.XamlRoot.RasterizationScale;
        var desiredWidth = WindowWidth;

        // Re-measure at the final width so wrap-sensitive content reports its actual height.
        RootGrid.Measure(new Windows.Foundation.Size(desiredWidth, double.PositiveInfinity));

        var desiredHeight = RootGrid.DesiredSize.Height;
        if (ExtendsContentIntoTitleBar) desiredHeight -= 30;

        AppWindow.ResizeClient(new SizeInt32((int)Math.Ceiling(desiredWidth * dpiScale), (int)Math.Ceiling(desiredHeight * dpiScale)));
        this.CenterOnScreen();
    }

    // The sticker picker returned a sticker: insert it into the editor and record its usage.
    private async void OnViewModelStickerSelected(object sender, StickerContent stickerContent)
    {
        var inserted = await PostEditor.InsertStickerAsync(stickerContent);
        if (!inserted)
        {
            await _viewModel.ShowMessageDialogAsync(new MessageDialogParameters(Constants.ErrorTitle, Constants.StickerLoadErrorMessage));
            return;
        }

        _ = _viewModel.ExecuteRequestAsync(new RecordStickerUsage(stickerContent.StickerId, stickerContent.StickerContentId));
        PostEditor.FocusEditor();
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

    // Window-wide file drops are accepted only while the composer can take new media; the
    // loading overlay blocks the window and the view model owns the mode and slot rules.
    private bool CanAcceptDroppedMediaFiles => LoadingGrid.Visibility == Visibility.Collapsed && _viewModel.CanAcceptMediaFiles;

    // File drags are the only ones the window reacts to, so the attachment reorder and text
    // drags keep their own behavior. The cursor shows copy with a caption while the composer
    // can take media and a blocked cursor while it cannot.
    private void OnRootGridDragOver(object sender, DragEventArgs e)
    {
        if (!e.DataView.Contains(StandardDataFormats.StorageItems)) return;

        e.AcceptedOperation = CanAcceptDroppedMediaFiles ? DataPackageOperation.Copy : DataPackageOperation.None;
        if (e.AcceptedOperation != DataPackageOperation.Copy) return;

        if (e.DragUIOverride is { } dragUIOverride)
        {
            dragUIOverride.Caption = "사진/영상 첨부";
            dragUIOverride.IsCaptionVisible = true;
        }
    }

    // Receives the dropped storage files and hands their paths to the view model, which
    // filters the supported formats and reports the excluded files.
    private async void OnRootGridDrop(object sender, DragEventArgs e)
    {
        if (!e.DataView.Contains(StandardDataFormats.StorageItems) || !CanAcceptDroppedMediaFiles) return;

        e.Handled = true;
        var deferral = e.GetDeferral();
        try
        {
            var storageItems = await e.DataView.GetStorageItemsAsync();
            var sourcePaths = storageItems.OfType<StorageFile>().Select(file => file.Path).Where(path => !string.IsNullOrEmpty(path)).ToList();
            if (sourcePaths.Count == 0) return;
            await _viewModel.AddDroppedMediaFilesAsync(sourcePaths);
        }
        finally { deferral.Complete(); }
    }

    protected override async void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        base.OnWindowLoaded(sender, e);

        PostEditor.Initialize(_viewModel);
        // A hashtag tap opens the composer with the tapped tag pre-inserted.
        foreach (var hashtag in _viewModel.InitialHashtags) PostEditor.AppendHashtag(new HashtagContent { Tag = hashtag });
        _viewModel.LoadIsKakaoPostEnabledSetting();
        _viewModel.LoadIsTimelineRefreshEnabledSetting();
        PostEditor.IsKakaoMentionMode = _viewModel.IsKakaoMode;
        if (_viewModel.IsKakaoWriteMode)
        {
            Title = "카카오스토리 게시글 작성";
            AppTitleBar.Title = "카카오스토리 게시글 작성";
            CaptionTextBlock.Text = "카카오스토리와 히스토리에 함께 게시됩니다.";
        }
        else if (_viewModel.IsKakaoEditMode)
        {
            await PostEditor.SetContentsAsync(_viewModel.KakaoEditorContents);
            Title = "카카오스토리 게시글 수정";
            AppTitleBar.Title = "카카오스토리 게시글 수정";
            SubmitButton.Content = "수정";
            CaptionTextBlock.Text = "카카오스토리에 게시할 내용을 수정하세요.";
            PostEditor.PlaceholderText = "수정할 내용을 입력하세요";
        }
        else if (_viewModel.IsKakaoShareMode)
        {
            Title = "카카오스토리 게시글 공유";
            AppTitleBar.Title = "카카오스토리 게시글 공유";
            SubmitButton.Content = "공유";
            CaptionTextBlock.Text = "원하는 친구와 나누고 싶은 이야기를 적어보세요.";
            PostEditor.PlaceholderText = "공유할 내용을 입력하세요";
        }
        else if (_viewModel.IsShareMode)
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

    private void OnCloseKeyInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        Close();
    }

    // The view model finished the write successfully: close the composer.
    private void OnSubmitCompleted(object sender, EventArgs e) => Close();

    // Collects the editor contents and hands them to the submit flow.
    private async void OnSubmitButtonClicked(object sender, RoutedEventArgs e) => await _viewModel.SubmitAsync(PostEditor.Text, PostEditor.GetContents());

    // Clears the static instance on close so the closed composer and its content tree are not
    // kept alive for the rest of the app session.
    protected override void OnWindowClosed(object sender, WindowEventArgs args)
    {
        if (ReferenceEquals(s_instance, this)) s_instance = null;

        base.OnWindowClosed(sender, args);
    }

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
