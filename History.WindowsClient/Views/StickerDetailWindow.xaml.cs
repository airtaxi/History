using History.WindowsClient.Helpers;
using History.WindowsClient.Models;
using History.WindowsClient.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using WinUIEx;

namespace History.WindowsClient.Views;

// Sticker detail window hosting the sticker detail view model. Subclasses BaseWindow so the
// theme, icon, centering, and loading-message routing apply automatically; the view model's
// dialog/loading events are fulfilled directly on this window's content.
public sealed partial class StickerDetailWindow : BaseWindow
{
    // Kakao Story emoticons carry no sticker id, so their detail is not available yet.
    private const string EmoticonNoticeTitle = "안내";
    private const string EmoticonNoticeMessage = "카카오스토리 이모티콘은 스티커 상세를 지원하지 않습니다.";

    public StickerDetailWindowViewModel ViewModel { get; }

    public StickerDetailWindow(StickerDetailWindowViewModel viewModel) : base(viewModel)
    {
        ViewModel = viewModel;

        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        this.CenterOnScreen();
    }

    // Opens the sticker detail window for the given sticker id, modal to the owner window.
    public static StickerDetailWindow ShowModal(string stickerId, Window ownerWindow)
    {
        var window = new StickerDetailWindow(new StickerDetailWindowViewModel(stickerId));

        window.ActivateModal(ownerWindow);
        return window;
    }

    // Reports the unsupported Kakao Story emoticon case on the given dialog surface.
    public static async Task ShowEmoticonNoticeAsync(BaseViewModel baseViewModel)
    {
        if (baseViewModel == null) return;

        await baseViewModel.ShowMessageDialogAsync(new MessageDialogParameters(EmoticonNoticeTitle, EmoticonNoticeMessage));
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
        DetailScrollViewer.IsEnabled = visibility == Visibility.Collapsed;
        LoadingTextBlock.Text = message;
    }

    protected override void SubscribeViewModelEvents()
    {
        base.SubscribeViewModelEvents();

        ViewModel.CloseRequested += OnViewModelCloseRequested;
    }

    protected override void UnsubscribeViewModelEvents()
    {
        ViewModel.CloseRequested -= OnViewModelCloseRequested;

        base.UnsubscribeViewModelEvents();
    }

    protected override async void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        base.OnWindowLoaded(sender, e);

        // A failed load (the sticker is gone) closes the window instead of leaving an empty
        // detail behind.
        if (!await ViewModel.InitializeAsync())
        {
            Close();
            return;
        }

        Title = ViewModel.Name;
        AppTitleBar.Title = ViewModel.Name;

        Activate();
    }

    // The view model finished the delete flow: close the detail window.
    private void OnViewModelCloseRequested(object sender, EventArgs e) => Close();

    // Escape closes the asset preview overlay first, then the window itself.
    private void OnCloseKeyInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;

        if (ViewModel.IsAssetPreviewVisible)
        {
            ViewModel.CloseAssetPreview();
            return;
        }

        Close();
    }
}
