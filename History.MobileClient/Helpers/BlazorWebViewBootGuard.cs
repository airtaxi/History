using Microsoft.AspNetCore.Components.WebView.Maui;

namespace History.MobileClient.Helpers;

// A BlazorWebView that survives an Android activity recreation (swipe-away from recents,
// multi-window close) can reconnect without completing the Blazor boot handshake, which
// leaves index.html's "Loading..." placeholder on screen forever. timelineInterop.init
// sets window.__blazorBooted only after the Blazor runtime has booted and rendered, so a
// missing marker means the boot never completed. This guard checks the marker after each
// handler (re)connect and each window resume, and issues at most one reload per trigger
// when the marker is absent.
public static class BlazorWebViewBootGuard
{
    private const string BootedCheckScript = "window.__blazorBooted === true ? 'true' : 'false'";
    private static readonly TimeSpan BootCheckDelay = TimeSpan.FromSeconds(3);

    public static void Attach(BlazorWebView webView)
    {
        webView.HandlerChanged += OnHandlerChanged;
        SubscribeToWindowResumed(webView);

        void OnHandlerChanged(object sender, EventArgs e)
        {
            if (webView.Handler is not null) ScheduleBootCheck(webView);
        }
    }

    private static void SubscribeToWindowResumed(BlazorWebView webView)
    {
        if (App.MainWindow is null) return;

        // Self-cleaning weak subscription: popped pages drop their resume handler on the
        // next resume instead of leaking through the window's event list.
        var weakWebView = new WeakReference<BlazorWebView>(webView);
        EventHandler onResumed = null;
        onResumed = (sender, args) =>
        {
            if (!weakWebView.TryGetTarget(out var target) || target.Handler is null)
            {
                App.MainWindow.Resumed -= onResumed;
                return;
            }
            ScheduleBootCheck(target);
        };
        App.MainWindow.Resumed += onResumed;
    }

    private static void ScheduleBootCheck(BlazorWebView webView) => _ = CheckAndRecoverAsync(webView);

    private static async Task CheckAndRecoverAsync(BlazorWebView webView)
    {
        try
        {
            await Task.Delay(BootCheckDelay);
            if (webView.Handler is null) return;

            if (await IsBootedAsync(webView)) return;

            await MainThread.InvokeOnMainThreadAsync(() => Reload(webView));

            // Observe the reload result once; a second failure is left for the next
            // trigger so a permanently stuck webview does not reload in a loop.
            await Task.Delay(BootCheckDelay);
            if (webView.Handler is null) return;
            await IsBootedAsync(webView);
        }
        catch (Exception)
        {
            // The webview may be mid-disconnect (page popped, activity destroyed);
            // the next handler connect or window resume retries the check.
        }
    }

    private static async Task<bool> IsBootedAsync(BlazorWebView webView)
    {
        try
        {
            var result = await MainThread.InvokeOnMainThreadAsync(() => EvaluateJavaScriptAsync(webView, BootedCheckScript));
            return result is "true" or "\"true\"" or "1";
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static Task<string> EvaluateJavaScriptAsync(BlazorWebView webView, string script)
    {
#if ANDROID
        if (webView.Handler?.PlatformView is Android.Webkit.WebView androidWebView)
        {
            var taskCompletionSource = new TaskCompletionSource<string>();
            androidWebView.EvaluateJavascript(script, new JavaScriptValueCallback(taskCompletionSource));
            return taskCompletionSource.Task;
        }
#elif IOS
        if (webView.Handler?.PlatformView is WebKit.WKWebView iosWebView) return EvaluateJavaScriptAsync(iosWebView, script);
#endif
        return Task.FromResult(string.Empty);
    }

#if IOS
    private static async Task<string> EvaluateJavaScriptAsync(WebKit.WKWebView webView, string script)
    {
        var result = await webView.EvaluateJavaScriptAsync(script);
        return result?.ToString() ?? string.Empty;
    }
#endif

#if ANDROID
    private sealed class JavaScriptValueCallback : Java.Lang.Object, Android.Webkit.IValueCallback
    {
        private readonly TaskCompletionSource<string> _taskCompletionSource;

        public JavaScriptValueCallback(TaskCompletionSource<string> taskCompletionSource) => _taskCompletionSource = taskCompletionSource;

        public void OnReceiveValue(Java.Lang.Object result)
        {
            if (result is null) _taskCompletionSource.TrySetResult(string.Empty);
            else _taskCompletionSource.TrySetResult(result.ToString());
        }
    }
#endif

    private static void Reload(BlazorWebView webView)
    {
#if ANDROID
        if (webView.Handler?.PlatformView is Android.Webkit.WebView androidWebView) androidWebView.Reload();
#elif IOS
        if (webView.Handler?.PlatformView is WebKit.WKWebView iosWebView) iosWebView.Reload();
#endif
    }
}