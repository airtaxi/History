using History.MobileClient.Components.Timeline;
using History.MobileClient.Helpers;
using History.MobileClient.ViewModels;
using Microsoft.AspNetCore.Components.WebView.Maui;

namespace History.MobileClient.Pages;

public partial class BlazorPublicPostsPage : ContentPage
{
    private readonly PublicPostsViewModel _viewModel = new();

    public BlazorPublicPostsPage()
    {
        InitializeComponent();
        BindingContext = _viewModel;

        MainBlazorWebView.RootComponents.Add(new RootComponent
        {
            Selector = "#app",
            ComponentType = typeof(PublicPosts),
            Parameters = new Dictionary<string, object>
            {
                [nameof(PublicPosts.ViewModel)] = _viewModel
            }
        });

#if ANDROID
        // Long-press copy is handled by the app itself (timelineInterop.attachLongPress),
        // so suppress the native webview long-click haptic.
        MainBlazorWebView.HandlerChanged += OnMainBlazorWebViewHandlerChanged;
#endif
    }

    #if ANDROID
    private void OnMainBlazorWebViewHandlerChanged(object sender, EventArgs e)
    {
        if (MainBlazorWebView.Handler?.PlatformView is not Android.Webkit.WebView webView) return;

        webView.HapticFeedbackEnabled = false;

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

        if (MainBlazorWebView.Handler?.PlatformView is Android.Webkit.WebView webView) ApplyWebViewSize(webView);
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

    protected override void OnAppearing()
    {
        base.OnAppearing();

        var safeAreaTopHeight = LayoutHelper.GetSafeAreaTopHeight();
        if (safeAreaTopHeight != 0)
        {
            var statusBarHeight = LayoutHelper.GetStatusBarHeight();
            Padding = new Thickness(Padding.Left, -(safeAreaTopHeight - statusBarHeight), Padding.Right, Padding.Bottom);
        }

        _ = _viewModel.OnAppearingAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.OnDisappearing();
    }

    private void OnLoaded(object sender, EventArgs e)
    {
#if IOS
        AppleSwipeGestureHelper.ApplyToPage(this);
#endif
    }

    private async void OnTitleGridTapped(object sender, TappedEventArgs e) => await _viewModel.RefreshAsync();

    private void OnScrollToTopBorderTapped(object sender, TappedEventArgs e) => _viewModel.RequestScrollToTop();

    private async void OnBackImageTapped(object sender, TappedEventArgs e) => await App.PopAsync();
}
