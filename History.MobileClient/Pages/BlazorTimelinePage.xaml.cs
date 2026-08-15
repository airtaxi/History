using History.MobileClient.Components.Timeline;
using History.MobileClient.Helpers;
using History.MobileClient.ViewModels;
using Microsoft.AspNetCore.Components.WebView.Maui;

namespace History.MobileClient.Pages;

public partial class BlazorTimelinePage : ContentPage
{
    private readonly TimelineViewModel _viewModel = new();

#if IOS
    private Thickness _scrollToTopBorderBaseMargin;
    private Thickness _writePostBorderBaseMargin;
#endif

    public BlazorTimelinePage()
    {
        InitializeComponent();
        BindingContext = _viewModel;

        TimelineBlazorWebView.RootComponents.Add(new RootComponent
        {
            Selector = "#app",
            ComponentType = typeof(Timeline),
            Parameters = new Dictionary<string, object?>
            {
                [nameof(Timeline.ViewModel)] = _viewModel
            }
        });

        _viewModel.ModeChanged += OnModeChanged;

#if ANDROID || IOS
        // Android: suppress the webview long-click haptic (timelineInterop.attachLongPress
        // handles copy) and install the kakao emoticon interceptor. iOS: register the
        // kakao emoticon scheme handler.
        TimelineBlazorWebView.HandlerChanged += OnTimelineBlazorWebViewHandlerChanged;
#endif

#if IOS
        // Capture the original XAML margins before any tab bar inset is applied.
        _scrollToTopBorderBaseMargin = ScrollToTopBorder.Margin;
        _writePostBorderBaseMargin = WritePostBorder.Margin;

        RootGrid.SafeAreaEdges = new(SafeAreaRegions.Default, SafeAreaRegions.Default, SafeAreaRegions.Default, SafeAreaRegions.SoftInput);
#endif
    }

    private void OnModeChanged(bool isKakaoStoryMode) => SearchImage.IsVisible = !isKakaoStoryMode;

#if ANDROID
    private void OnTimelineBlazorWebViewHandlerChanged(object sender, EventArgs e)
    {
        if (TimelineBlazorWebView.Handler?.PlatformView is not Android.Webkit.WebView webView) return;

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

        if (TimelineBlazorWebView.Handler?.PlatformView is Android.Webkit.WebView webView) ApplyWebViewSize(webView);
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
    private void OnTimelineBlazorWebViewHandlerChanged(object sender, EventArgs e)
    {
        // Kakao emoticon images require the story.kakao.com Referer header, which
        // WKWebView's <img> loader never sends. Razor rewrites those URLs to a
        // custom scheme; register the handler that fetches them with the Referer.
        if (TimelineBlazorWebView.Handler?.PlatformView is WebKit.WKWebView webView)
            KakaoEmoticonUrlSchemeHandler.EnsureRegistered(webView.Configuration);
    }
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
        // Apply the tab bar height as bottom margin here, once the native
        // tab bar has been laid out and CustomTabBarAppearanceTracker has captured
        // its height. Falls back to 49pt when the tab bar cannot be resolved yet.
        var tabBarHeight = LayoutHelper.GetTabBarHeight();

        ScrollToTopBorder.Margin = new Thickness(_scrollToTopBorderBaseMargin.Left, _scrollToTopBorderBaseMargin.Top, _scrollToTopBorderBaseMargin.Right, _scrollToTopBorderBaseMargin.Bottom + tabBarHeight);
        WritePostBorder.Margin = new Thickness(_writePostBorderBaseMargin.Left, _writePostBorderBaseMargin.Top, _writePostBorderBaseMargin.Right, _writePostBorderBaseMargin.Bottom + tabBarHeight);
#endif

        _ = _viewModel.OnAppearingAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.OnDisappearing();
    }

    private async void OnTitleGridTapped(object sender, TappedEventArgs e) => await _viewModel.RefreshAsync();

    private async void OnWritePostBorderTapped(object sender, TappedEventArgs e) => await _viewModel.WritePostCommand.ExecuteAsync(null);

    private void OnScrollToTopBorderTapped(object sender, TappedEventArgs e) => _viewModel.RequestScrollToTop();

    private async void OnSearchPostImageTapped(object sender, TappedEventArgs e) => await _viewModel.SearchCommand.ExecuteAsync(null);
}
