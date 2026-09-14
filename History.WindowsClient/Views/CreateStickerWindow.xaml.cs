using History.WindowsClient.Helpers;
using History.WindowsClient.ViewModels.Extras;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using WinUIEx;

namespace History.WindowsClient.Views;

// Sticker create window hosting the sticker create view model. Subclasses BaseWindow so the
// theme, icon, centering, and loading-message routing apply automatically; the view model's
// dialog/picker/loading events are fulfilled directly on this window's content.
public sealed partial class CreateStickerWindow : BaseWindow
{
    public CreateStickerWindowViewModel ViewModel { get; }

    public CreateStickerWindow(CreateStickerWindowViewModel viewModel) : base(viewModel)
    {
        ViewModel = viewModel;

        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        this.CenterOnScreen();
    }

    // Opens the sticker create window modally over the owner window.
    public static CreateStickerWindow ShowModal(Window ownerWindow)
    {
        var window = new CreateStickerWindow(new CreateStickerWindowViewModel());

        window.ActivateModal(ownerWindow);
        return window;
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
        ImportExternalButton.IsEnabled = visibility == Visibility.Collapsed;
        FormScrollViewer.IsEnabled = visibility == Visibility.Collapsed;
        CreateButton.IsEnabled = visibility == Visibility.Collapsed;
        LoadingTextBlock.Text = message;
    }

    protected override void SubscribeViewModelEvents()
    {
        base.SubscribeViewModelEvents();

        ViewModel.Created += OnViewModelCreated;
    }

    protected override void UnsubscribeViewModelEvents()
    {
        ViewModel.Created -= OnViewModelCreated;

        base.UnsubscribeViewModelEvents();
    }

    protected override void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        base.OnWindowLoaded(sender, e);

        Activate();
    }

    // The view model finished the create flow: close the create window.
    private void OnViewModelCreated(object sender, EventArgs e) => Close();

    private void OnCloseKeyInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        Close();
    }
}
