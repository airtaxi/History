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

public sealed partial class MainWindow : Window,
    IRecipient<LoadingStateRequestedMessage>,
    IRecipient<ShowLoadingMessage>,
    IRecipient<HideLoadingMessage>
{
    private static MainWindow s_instance;

    private readonly ApplicationThemeService _applicationThemeService = App.Services.GetRequiredService<ApplicationThemeService>();

    public static Frame Frame => s_instance.AppFrame;

    public MainWindow()
    {
        s_instance = this;

        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        _applicationThemeService.ApplyThemeToWindow(this);
        _applicationThemeService.ThemeChanged += OnApplicationThemeServiceThemeChanged;

        this.CenterOnScreen();

        AppWindow.SetIcon("Assets/Icon.ico");

        WeakReferenceMessenger.Default.Register((IRecipient<LoadingStateRequestedMessage>) this);
        WeakReferenceMessenger.Default.Register((IRecipient<ShowLoadingMessage>) this);
        WeakReferenceMessenger.Default.Register((IRecipient<HideLoadingMessage>) this);

        AppFrame.Navigate(typeof(LoginPage));
    }

    public static void SetForegroundWindow() => s_instance.SetForegroundWindow();

    private static void ShowLoading(string message = null)
    {
        if (s_instance.DispatcherQueue.HasThreadAccess) SetLoadingState(Visibility.Visible, message);
        else s_instance.DispatcherQueue.TryEnqueue(() => SetLoadingState(Visibility.Visible, message));
    }

    private static void HideLoading()
    {
        if (s_instance.DispatcherQueue.HasThreadAccess) SetLoadingState(Visibility.Collapsed, null);
        else s_instance.DispatcherQueue.TryEnqueue(() => SetLoadingState(Visibility.Collapsed, null));
    }

    private static void SetLoadingState(Visibility visibility, string message)
    {
        s_instance.LoadingGrid.Visibility = visibility;
        if (!string.IsNullOrEmpty(message) || visibility == Visibility.Visible)
        {
            s_instance.AppTitleBar.IsEnabled = false;
            s_instance.AppFrame.IsEnabled = false;
            s_instance.LoadingTextBlock.Text = message;
            s_instance.LoadingTextBlock.Visibility = Visibility.Visible;
        }
        else
        {
            s_instance.AppTitleBar.IsEnabled = true;
            s_instance.AppFrame.IsEnabled = true;
            s_instance.LoadingTextBlock.Visibility = Visibility.Collapsed;
            s_instance.LoadingTextBlock.Text = "";
        }
    }

    public static void SetAppTitleBarIsPaneToggleButtonVisible(bool isOn) => s_instance.DispatcherQueue.TryEnqueue(() => s_instance.AppTitleBar.IsPaneToggleButtonVisible = isOn);

    private void OnAppFrameNavigated(object sender, NavigationEventArgs e)
    {
        var frame = sender as Frame;

        if (e.SourcePageType == typeof(MainPage) || e.SourcePageType == typeof(LoginPage)) frame.BackStack.Clear();
        AppTitleBar.IsBackButtonVisible = frame.CanGoBack;
    }

    private void OnAppTitleBarPaneToggleRequested(Microsoft.UI.Xaml.Controls.TitleBar sender, object args) => WeakReferenceMessenger.Default.Send(new ToggleNavigationPaneMessage());

    private void OnAppTitleBarBackRequested(Microsoft.UI.Xaml.Controls.TitleBar sender, object args)
    {
        if (AppFrame.CanGoBack)
        {
            AppFrame.GoBack();
        }
    }

    private void OnApplicationThemeServiceThemeChanged(ElementTheme theme) => _applicationThemeService.ApplyThemeToWindow(this);

    // Runs loading requests that originated from this window's pages/controls: the
    // XamlRoot reference comparison routes messages from other windows away.
    public void Receive(LoadingStateRequestedMessage message)
    {
        if (Content.XamlRoot != message.XamlRoot) return;

        _ = RunLoadingAsync(message);
    }

    public void Receive(ShowLoadingMessage message) => ShowLoading(message.LoadingMessage);

    public void Receive(HideLoadingMessage message) => HideLoading();

    private async Task RunLoadingAsync(LoadingStateRequestedMessage message)
    {
        try
        {
            ShowLoading(message.LoadingMessage);
            await message.Action();
            message.Complete();
        }
        catch (Exception exception) { message.Fail(exception); }
        finally { HideLoading(); }
    }
}
