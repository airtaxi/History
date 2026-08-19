using CommunityToolkit.Mvvm.Messaging;
using History.MobileClient.Components.Profile;
using History.MobileClient.Helpers;
using History.MobileClient.Messages;
using History.MobileClient.ViewModels;
using Microsoft.AspNetCore.Components.WebView;
using Microsoft.AspNetCore.Components.WebView.Maui;
using System.ComponentModel;
using UraniumUI.Icons.MaterialSymbols;

namespace History.MobileClient.Pages;

public partial class BlazorUserPage : ContentPage
{
    private readonly UserProfileViewModel _viewModel;

#if IOS
    private Thickness _scrollToTopBorderBaseMargin;
    private Thickness _writePostBorderBaseMargin;
#endif

    public BlazorUserPage() : this(new UserProfileViewModel())
    {
        Shell.SetTabBarIsVisible(this, true);
    }

    public BlazorUserPage(string userId) : this(new UserProfileViewModel(userId)) { }

    public BlazorUserPage(string userId, bool isKakaoStoryMode) : this(new UserProfileViewModel(userId, isKakaoStoryMode)) { }

    public BlazorUserPage(string userId, bool isKakaoStoryMode, bool showPillGrid) : this(new UserProfileViewModel(userId, isKakaoStoryMode, showPillGrid)) { }

    private BlazorUserPage(UserProfileViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        BindingContext = _viewModel;

        UserBlazorWebView.RootComponents.Add(new RootComponent
        {
            Selector = "#app",
            ComponentType = typeof(Profile),
            Parameters = new Dictionary<string, object>
            {
                [nameof(Profile.ViewModel)] = _viewModel
            }
        });

        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        WeakReferenceMessenger.Default.Register<BlazorWebViewHibernationMessage>(this, OnBlazorWebViewHibernationMessageReceived);

#if ANDROID
        // Suppress the webview long-click haptic (timelineInterop.attachLongPress
        // handles copy) and install the kakao emoticon interceptor.
        UserBlazorWebView.HandlerChanged += OnUserBlazorWebViewHandlerChanged;
#endif

#if IOS
        // The kakao emoticon scheme handler must be registered before the WKWebView is
        // created, because WKWebView copies its configuration at creation time and
        // ignores later mutations. BlazorWebViewInitializing exposes the pre-creation
        // configuration for exactly this purpose.
        UserBlazorWebView.BlazorWebViewInitializing += OnUserBlazorWebViewInitializing;

        // Capture the original XAML margins before any tab bar inset is applied.
        _scrollToTopBorderBaseMargin = ScrollToTopBorder.Margin;
        _writePostBorderBaseMargin = WritePostBorder.Margin;

        RootGrid.SafeAreaEdges = new(SafeAreaRegions.Default, SafeAreaRegions.Default, SafeAreaRegions.Default, SafeAreaRegions.SoftInput);
#endif
    }

    private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(UserProfileViewModel.UseGridLayout)) UpdateLayoutGlyph();
    }

    private void UpdateLayoutGlyph() => LayoutFontImageSource.Glyph = _viewModel.UseGridLayout ? MaterialSharp.Lists : MaterialSharp.Dataset;

    private void OnBlazorWebViewHibernationMessageReceived(object recipient, BlazorWebViewHibernationMessage message)
    {
#if ANDROID
        Android.Util.Log.Info("BlazorHibernation", $"[{DateTime.Now:HH:mm:ss.fff}] hibernation={message.Value}, platform view is webview: {UserBlazorWebView.Handler?.PlatformView is Android.Webkit.WebView}");

        // WebView.OnPause halts JS timers, animations, and video playback, so a
        // backgrounded Blazor tab cannot keep burning CPU while the realtime
        // foreground service keeps the process alive.
        if (UserBlazorWebView.Handler?.PlatformView is Android.Webkit.WebView webView)
        {
            if (message.Value) webView.OnPause();
            else webView.OnResume();
        }
#endif
    }

#if ANDROID
    private void OnUserBlazorWebViewHandlerChanged(object sender, EventArgs e)
    {
        if (UserBlazorWebView.Handler?.PlatformView is not Android.Webkit.WebView webView) return;

        webView.HapticFeedbackEnabled = false;

        // Kakao emoticon images require the story.kakao.com Referer header, which the
        // webview's <img> loader never sends. Intercept and re-issue those requests
        // while delegating everything else to MAUI's own webview client.
        if (webView.WebViewClient is not KakaoEmoticonWebViewClient)
            webView.SetWebViewClient(new KakaoEmoticonWebViewClient(webView.WebViewClient));

        // Inline videos are swapped in by the viewmodel after the user's tap, so the
        // gesture happens before the <video> element exists. Allow muted autoplay
        // without a user gesture or the video never starts (gray box + play symbol).
        webView.Settings.MediaPlaybackRequiresUserGesture = false;

        // The webview's own background is white by default and flashes before the page
        // paints. Match it to the app theme's page background so dark mode doesn't
        // flicker white during load.
        var isDark = Utils.GetGlobalAppTheme() == AppTheme.Dark;
        webView.SetBackgroundColor(isDark ? Android.Graphics.Color.ParseColor("#1F1F1F") : Android.Graphics.Color.ParseColor("#F0F0F0"));

        // The webview can miss its size when the handler connects before the first
        // layout pass; re-apply once the layout pass has run.
        webView.Post(() => ApplyWebViewSize(webView));
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        if (UserBlazorWebView.Handler?.PlatformView is Android.Webkit.WebView webView) ApplyWebViewSize(webView);
    }

    private static void ApplyWebViewSize(Android.Webkit.WebView webView)
    {
        if (webView.Parent is not Android.Views.View parent) return;

        var parentWidth = parent.Width;
        var parentHeight = parent.Height;
        if (parentWidth <= 0 || parentHeight <= 0) return;

        Android.Util.Log.Info("BlazorSize", $"webview {webView.Width}x{webView.Height} parent {parentWidth}x{parentHeight}");
        if (webView.Width != parentWidth || webView.Height != parentHeight) webView.Layout(0, 0, parentWidth, parentHeight);
    }
#endif

#if IOS
    // Kakao emoticon images require the story.kakao.com Referer header, which
    // WKWebView's <img> loader never sends. Razor rewrites those URLs to a
    // custom scheme; registering the handler on the pre-creation configuration
    // is what makes WKWebView actually route those requests to it.
    private void OnUserBlazorWebViewInitializing(object sender, BlazorWebViewInitializingEventArgs e) => KakaoEmoticonUrlSchemeHandler.EnsureRegistered(e.Configuration);
#endif

    protected override void OnAppearing()
    {
        base.OnAppearing();

        var safeAreaTopHeight = LayoutHelper.GetSafeAreaTopHeight();
        if (safeAreaTopHeight != 0)
        {
            var statusBarHeight = LayoutHelper.GetStatusBarHeight();
            Padding = new Thickness(Padding.Left, -(safeAreaTopHeight - statusBarHeight), Padding.Right, Padding.Bottom);
        }

#if IOS
        // Only apply tab bar inset when the tab bar is visible (my profile).
        // Other users' profiles hide the tab bar (Shell.TabBarIsVisible="False"),
        // so the floating borders must not be offset.
        if (_viewModel.IsMyProfileTab)
        {
            var tabBarHeight = LayoutHelper.GetTabBarHeight();

            ScrollToTopBorder.Margin = new Thickness(_scrollToTopBorderBaseMargin.Left, _scrollToTopBorderBaseMargin.Top, _scrollToTopBorderBaseMargin.Right, _scrollToTopBorderBaseMargin.Bottom + tabBarHeight);
            WritePostBorder.Margin = new Thickness(_writePostBorderBaseMargin.Left, _writePostBorderBaseMargin.Top, _writePostBorderBaseMargin.Right, _writePostBorderBaseMargin.Bottom + tabBarHeight);
        }
#endif

        _ = _viewModel.OnAppearingAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.OnDisappearing();
    }

    protected override bool OnBackButtonPressed()
    {
        if (_viewModel.IsMyProfileTab) return false;

        _ = App.PopAsync();
        return true;
    }

    private async void OnBackImageTapped(object sender, TappedEventArgs e) => await _viewModel.BackAsync();

    private async void OnTitleLabelTapped(object sender, TappedEventArgs e) => await _viewModel.RefreshAsync();

    private void OnLayoutImageTapped(object sender, TappedEventArgs e) => _viewModel.ToggleLayout();

    private async void OnLinkImageTapped(object sender, TappedEventArgs e) => await _viewModel.CopyProfileLinkAsync();

    private async void OnMessageImageTapped(object sender, TappedEventArgs e) => await _viewModel.MessageAsync();

    private async void OnMemoImageTapped(object sender, TappedEventArgs e) => await _viewModel.MemoAsync();

    private async void OnFriendsImageTapped(object sender, TappedEventArgs e) => await _viewModel.FriendsAsync();

    private async void OnBanUserImageTapped(object sender, TappedEventArgs e) => await _viewModel.BanAsync();

    private async void OnSettingsImageTapped(object sender, TappedEventArgs e) => await _viewModel.SettingsAsync();

    private async void OnWritePostBorderTapped(object sender, TappedEventArgs e) => await _viewModel.WritePostAsync();

    private void OnScrollToTopBorderTapped(object sender, TappedEventArgs e) => _viewModel.RequestScrollToTop();

    private void OnLoaded(object sender, EventArgs e)
    {
#if IOS
        if (!_viewModel.IsMyProfileTab)
        {
            AppleSwipeGestureHelper.ApplyToPage(this);
        }
#endif
    }
}
