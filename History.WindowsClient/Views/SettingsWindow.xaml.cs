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

    public SettingsWindow(SettingsWindowViewModel viewModel) : base()
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
        SettingsScrollViewer.IsEnabled = visibility == Visibility.Collapsed;
        LoadingTextBlock.Text = message;
    }

    private void SubscribeViewModelEvents()
    {
        _viewModel.MessageDialogRequested += OnMessageDialogRequested;
        _viewModel.InputDialogRequested += OnInputDialogRequested;
        _viewModel.ContentDialogRequested += OnContentDialogRequested;
        _viewModel.SelectionDialogRequested += OnSelectionDialogRequested;
        _viewModel.LoadingStateRequested += OnLoadingStateRequested;
        _viewModel.ShowLoadingRequested += OnShowLoadingRequested;
        _viewModel.HideLoadingRequested += OnHideLoadingRequested;
        _viewModel.CloseRequested += OnViewModelCloseRequested;
        _viewModel.KakaoReloginRequested += OnKakaoReloginRequested;
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

    private void OnContentDialogRequested(object sender, ContentDialogRequestedEventArgs args)
    {
        var result = Content.ShowContentDialogAsync(args.Dialog);
        args.ResultTask = result;
    }

    private void OnSelectionDialogRequested(object sender, SelectionDialogRequestedEventArgs args)
    {
        var result = Content.ShowSelectionDialogAsync(args.Title, args.Options);
        args.ResultTask = result;
    }

    // Forwards the view model's loading requests to this window's overlay through the
    // weak-reference messenger; BaseWindow routes them by XamlRoot.
    private void OnLoadingStateRequested(object sender, LoadingStateRequestedEventArgs args) => LoadingStateRequestedMessage.Send(Content.XamlRoot, args);

    private void OnShowLoadingRequested(object sender, ShowLoadingRequestedEventArgs args) => ShowLoadingMessage.Send(args);

    private void OnHideLoadingRequested(object sender, HideLoadingRequestedEventArgs args) => HideLoadingMessage.Send();

    private void OnViewModelCloseRequested(object sender, EventArgs e) => Close();

    private async Task OnKakaoReloginRequested()
    {
        var loginWindow = new KakaoStoryLoginWindow(new KakaoStoryLoginWindowViewModel());
        loginWindow.MakeModal(this);
        await loginWindow.GetResultAsync();
    }

    private async void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.LoadAsync();
        Activate();
    }

    private void OnEscapeKeyInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        Close();
    }

    private void OnWindowClosed(object sender, WindowEventArgs args) => UnregisterMessengerRecipients();
}
