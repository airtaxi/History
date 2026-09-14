using History.WindowsClient.Helpers;
using History.WindowsClient.Messages;
using History.WindowsClient.Models;
using History.WindowsClient.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using WinUIEx;

namespace History.WindowsClient.Views;

// Settings window hosting the SettingsWindowViewModel. Subclasses BaseWindow so the theme,
// icon, centering, and loading-message routing apply automatically; the view model's
// dialog/loading events are fulfilled directly on this window's content. The Kakao Story
// login window is hosted as a modal over this window.
public sealed partial class SettingsWindow : BaseWindow
{
    private readonly SettingsWindowViewModel _viewModel;

    public SettingsWindowViewModel ViewModel => _viewModel;

    public SettingsWindow(SettingsWindowViewModel viewModel) : base(viewModel)
    {
        _viewModel = viewModel;

        InitializeComponent();

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
        if (DispatcherQueue.HasThreadAccess) SetLoadingState(Visibility.Collapsed, null);
        else DispatcherQueue.TryEnqueue(() => SetLoadingState(Visibility.Collapsed, null));
    }

    private void SetLoadingState(Visibility visibility, string message)
    {
        LoadingGrid.Visibility = visibility;
        AppTitleBar.IsEnabled = visibility == Visibility.Collapsed;
        SettingsScrollViewer.IsEnabled = visibility == Visibility.Collapsed;
        LoadingTextBlock.Text = message;
    }

    protected override void SubscribeViewModelEvents()
    {
        base.SubscribeViewModelEvents();

        _viewModel.CloseRequested += OnViewModelCloseRequested;
        _viewModel.KakaoReloginRequested += OnKakaoReloginRequested;
    }

    protected override void UnsubscribeViewModelEvents()
    {
        _viewModel.CloseRequested -= OnViewModelCloseRequested;
        _viewModel.KakaoReloginRequested -= OnKakaoReloginRequested;

        base.UnsubscribeViewModelEvents();
    }

    private void OnViewModelCloseRequested(object sender, EventArgs e) => Close();

    private async Task OnKakaoReloginRequested()
    {
        var loginWindow = new KakaoStoryLoginWindow(new KakaoStoryLoginWindowViewModel());
        loginWindow.MakeModal(this);
        await loginWindow.GetResultAsync();
    }

    protected override async void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        base.OnWindowLoaded(sender, e);

        await _viewModel.LoadAsync();
        Activate();
    }

    private void OnCloseKeyInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        Close();
    }
}
