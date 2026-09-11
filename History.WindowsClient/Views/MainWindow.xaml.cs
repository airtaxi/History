using CommunityToolkit.Mvvm.Messaging;
using History.Commons;
using History.WindowsClient.Controls;
using History.WindowsClient.Helpers;
using History.WindowsClient.Messages;
using History.WindowsClient.Models;
using History.WindowsClient.Pages;
using History.WindowsClient.Services;
using History.WindowsClient.ViewModels;
using History.WindowsClient.ViewModels.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using MongoDB.Driver.GridFS;
using System.ComponentModel;
using WinUIEx;

namespace History.WindowsClient.Views;

public sealed partial class MainWindow : BaseWindow,
    IRecipient<DiscoverModeChangedMessage>,
    IRecipient<KakaoStoryModeChangedMessage>
{
    private static MainWindow s_instance;
    private readonly NotificationsFlyoutViewModel _notificationsViewModel;

    public static MainWindow Instance => s_instance;

    public static Frame Frame => s_instance.AppFrame;

    public MainWindow() : base()
    {
        s_instance = this;

        InitializeComponent();

        // The flyout control is created with the window, so its view model is the single source
        // of truth for the unread notification count shown on the notification button badge.
        _notificationsViewModel = ((NotificationsFlyoutControl)NotificationsFlyout.Content).ViewModel;
        _notificationsViewModel.PropertyChanged += OnNotificationsViewModelPropertyChanged;

        WeakReferenceMessenger.Default.Register((IRecipient<DiscoverModeChangedMessage>)this);
        WeakReferenceMessenger.Default.Register((IRecipient<KakaoStoryModeChangedMessage>)this);

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        this.CenterOnScreen();

        AppFrame.Navigate(typeof(LoginPage));
    }

    public static void SetForegroundWindow() => s_instance.SetForegroundWindow();

    // Keeps the title bar toggle in sync with the left feed selected on the main page.
    public void Receive(DiscoverModeChangedMessage message) => DiscoverButton.IsChecked = message.Value;

    // The discover toggle only applies to the main page in History mode.
    public void Receive(KakaoStoryModeChangedMessage message) => UpdateDiscoverButtonVisibility();

    protected override void Navigate(Type pageType, object parameter)
    {
        // Ignore the request when the same user's profile is already showing in the frame: navigating
        // again is a meaningless action that would only consume more memory. The History and Kakao
        // Story profiles stay distinct even when their user id strings match.
        if (pageType == typeof(ProfilePage) && AppFrame.Content is ProfilePage currentProfilePage)
        {
            if (parameter is string userId && !currentProfilePage.IsKakaoStoryPage && currentProfilePage.UserId == userId) return;
            if (parameter is KakaoProfileParameters kakaoProfile && currentProfilePage.IsKakaoStoryPage && currentProfilePage.UserId == kakaoProfile.KakaoUserId) return;
        }

        AppFrame.Navigate(pageType, parameter);
    }

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
        // Refreshes the unread badge whenever the main page appears so the count is current right after login.
        if (e.SourcePageType == typeof(MainPage)) _ = _notificationsViewModel.RefreshAsync();
        var isToolbarVisible = e.SourcePageType == typeof(LoginPage) || e.SourcePageType == typeof(RegisterPage) || e.SourcePageType == typeof(BrowserPage) ? Visibility.Collapsed : Visibility.Visible;
        RefreshButton.Visibility = isToolbarVisible;
        NotificationsButton.Visibility = isToolbarVisible;
        ComposePostButton.Visibility = isToolbarVisible;
        MoreButton.Visibility = isToolbarVisible;
        UpdateDiscoverButtonVisibility();
    }

    // The discover toggle only applies to the main page in History mode, where the
    // left feed area can switch between the timeline and the discover page.
    private void UpdateDiscoverButtonVisibility() => DiscoverButton.Visibility = AppFrame.Content is MainPage && !CommonShared.LastUsedKakaoStoryMode ? Visibility.Visible : Visibility.Collapsed;

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

    private void OnDiscoverButtonClicked(object sender, RoutedEventArgs e) => WeakReferenceMessenger.Default.Send(new DiscoverModeToggleRequestedMessage());

    // Opens the compose window for the current mode: Kakao Story mode composes a new Kakao
    // Story post that mirrors to History, otherwise the History composer opens.
    private void OnComposePostButtonClicked(object sender, RoutedEventArgs e)
    {
        var settings = App.Services.GetRequiredService<ApplicationSettings>();
        var viewModel = CommonShared.LastUsedKakaoStoryMode ? new ComposePostWindowViewModel(settings, isKakaoWriteMode: true) : new ComposePostWindowViewModel(settings);
        new ComposePostWindow(viewModel).MakeModal(this);
    }

    // Refreshing on open keeps the flyout list current without polling. The login prompt is
    // enabled for the Kakao Story session so an expired session asks the user to re-login
    // only here, never during the quiet badge refresh.
    private void OnNotificationsFlyoutOpening(object sender, object e) => _ = ((NotificationsFlyoutControl)NotificationsFlyout.Content).ViewModel.RefreshAsync(promptLoginOnExpiredSession: true);

    // Keeps the notification button badge in sync with the flyout's unread notification count.
    private void OnNotificationsViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(NotificationsFlyoutViewModel.UnreadCount)) return;
        if (DispatcherQueue.HasThreadAccess) UpdateInfoBadgeValue(NotificationsInfoBadge, _notificationsViewModel.UnreadCount);
        else DispatcherQueue.TryEnqueue(() => UpdateInfoBadgeValue(NotificationsInfoBadge, _notificationsViewModel.UnreadCount));
    }

    private static void UpdateInfoBadgeValue(InfoBadge infoBadge, int value)
    {
        if (value > 0)
        {
            infoBadge.Value = value;
            if (infoBadge.Visibility == Visibility.Collapsed) infoBadge.Visibility = Visibility.Visible;
        }
        else if (infoBadge.Visibility == Visibility.Visible) infoBadge.Visibility = Visibility.Collapsed;
    }

    private void OnLeftHeaderButtonClicked(object sender, RoutedEventArgs e)
    {
        if (AppFrame.Content is MainPage) WeakReferenceMessenger.Default.Send(new RefreshButtonClickedMessage());
        else
        {
            AppFrame.Navigate(typeof(MainPage));
            AppFrame.BackStack.Clear();
        }
    }
}
