using CommunityToolkit.Mvvm.Messaging;
using History.WindowsClient.Messages;
using History.WindowsClient.Pages;
using History.WindowsClient.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using WinUIEx;

namespace History.WindowsClient.Views;

public sealed partial class MainWindow : BaseWindow
{
    private static MainWindow s_instance;

    public static MainWindow Instance => s_instance;

    public static Frame Frame => s_instance.AppFrame;

    public MainWindow() : base()
    {
        s_instance = this;

        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        this.CenterOnScreen();

        AppFrame.Navigate(typeof(LoginPage));
    }

    public static void SetForegroundWindow() => s_instance.SetForegroundWindow();

    protected override void Navigate(Type pageType, object parameter) => AppFrame.Navigate(pageType, parameter);

    protected override bool TryNavigateBack()
    {
        if (!AppFrame.CanGoBack) return false;

        AppFrame.GoBack();
        return true;
    }

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
        if (!string.IsNullOrEmpty(message) || visibility == Visibility.Visible)
        {
            AppTitleBar.IsEnabled = false;
            AppFrame.IsEnabled = false;
            LoadingTextBlock.Text = message;
            LoadingTextBlock.Visibility = Visibility.Visible;
        }
        else
        {
            AppTitleBar.IsEnabled = true;
            AppFrame.IsEnabled = true;
            LoadingTextBlock.Visibility = Visibility.Collapsed;
            LoadingTextBlock.Text = "";
        }
    }

    public static void SetAppTitleBarIsPaneToggleButtonVisible(bool isOn) => s_instance.DispatcherQueue.TryEnqueue(() => s_instance.AppTitleBar.IsPaneToggleButtonVisible = isOn);

    private void OnAppFrameNavigated(object sender, NavigationEventArgs e)
    {
        var frame = sender as Frame;

        if (e.SourcePageType == typeof(MainPage) || e.SourcePageType == typeof(LoginPage)) frame.BackStack.Clear();
        AppTitleBar.IsBackButtonVisible = frame.CanGoBack;
        MainSearchBox.Visibility = e.SourcePageType == typeof(MainPage) ? Visibility.Visible : Visibility.Collapsed;
        RefreshButton.Visibility = e.SourcePageType == typeof(LoginPage) || e.SourcePageType == typeof(RegisterPage) || e.SourcePageType == typeof(BrowserPage) ? Visibility.Collapsed : Visibility.Visible;
    }

    private void OnAppTitleBarPaneToggleRequested(Microsoft.UI.Xaml.Controls.TitleBar sender, object args) => WeakReferenceMessenger.Default.Send(new ToggleNavigationPaneMessage());

    private void OnAppTitleBarBackRequested(Microsoft.UI.Xaml.Controls.TitleBar sender, object args)
    {
        if (AppFrame.CanGoBack)
        {
            AppFrame.GoBack();
        }
    }

    private void OnMainSearchBoxQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args) => WeakReferenceMessenger.Default.Send(new MainWindowAutoSuggestBoxQuerySubmittedMessage(args.QueryText));

    private void OnRefreshButtonClicked(object sender, RoutedEventArgs e) => WeakReferenceMessenger.Default.Send(new RefreshButtonClickedMessage());
}
