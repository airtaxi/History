#if ANDROID

using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;

namespace History.MobileClient.Helpers;

// Kakao CDN emoticon URLs are hotlink-protected (allow_referer=story.kakao.com),
// and the webview's native <img> loader cannot send a custom Referer header.
// This client intercepts emoticon image requests and re-issues them with the
// Referer Kakao Story requires (the XAML FFImageLoading path does the same via
// its per-image configuration). Responses are cached in memory because
// intercepted responses bypass the webview's own HTTP cache.
//
// MAUI's BlazorWebView sets its own WebViewClient (WebKitWebViewClient) that
// serves the Blazor app content and runs the startup scripts from
// OnPageFinished, so this wrapper delegates every callback to it and only
// takes over emoticon image requests.
public sealed class KakaoEmoticonWebViewClient : Android.Webkit.WebViewClient
{
    private const string EmoticonUrlPrefix = "https://mk.kakaocdn.net/dna/emoticons";
    private const string KakaoStoryReferer = "https://story.kakao.com/";
    private const int CacheEntryLimit = 300;

    private static readonly HttpClient s_httpClient = new();
    private static readonly ConcurrentDictionary<string, byte[]> s_cache = new();

    private readonly Android.Webkit.WebViewClient _inner;

    public KakaoEmoticonWebViewClient(Android.Webkit.WebViewClient inner) => _inner = inner;

    public override Android.Webkit.WebResourceResponse ShouldInterceptRequest(Android.Webkit.WebView view, Android.Webkit.IWebResourceRequest request)
    {
        var url = request?.Url?.ToString();
        if (url == null || !url.StartsWith(EmoticonUrlPrefix, StringComparison.Ordinal))
            return _inner.ShouldInterceptRequest(view, request);

        if (s_cache.TryGetValue(url, out var cachedBytes)) return CreateResponse(cachedBytes);

        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Get, url);
            message.Headers.Referrer = new Uri(KakaoStoryReferer);

            // The synchronous HttpClient.Send overload is not supported by Android's
            // handler (PlatformNotSupportedException). Block on the async call instead
            // — ShouldInterceptRequest runs on the webview's IO thread without a
            // SynchronizationContext, so no deadlock is possible.
            using var response = s_httpClient.SendAsync(message).ConfigureAwait(false).GetAwaiter().GetResult();

            if (response.StatusCode != HttpStatusCode.OK) return _inner.ShouldInterceptRequest(view, request);

            using var responseStream = response.Content.ReadAsStream();
            using var memoryStream = new MemoryStream();
            responseStream.CopyTo(memoryStream);

            var bytes = memoryStream.ToArray();
            if (s_cache.Count >= CacheEntryLimit) s_cache.Clear();
            s_cache[url] = bytes;

            return CreateResponse(bytes);
        }
        catch
        {
            // Fetching with the Referer failed; fall back to the native request
            // (likely rejected by the CDN, but the page load must survive).
            return _inner.ShouldInterceptRequest(view, request);
        }
    }

    public override bool ShouldOverrideUrlLoading(Android.Webkit.WebView view, Android.Webkit.IWebResourceRequest request) =>
        _inner.ShouldOverrideUrlLoading(view, request);

    public override void OnPageFinished(Android.Webkit.WebView view, string url) => _inner.OnPageFinished(view, url);

    public override void DoUpdateVisitedHistory(Android.Webkit.WebView view, string url, bool isReload) => _inner.DoUpdateVisitedHistory(view, url, isReload);

    private static Android.Webkit.WebResourceResponse CreateResponse(byte[] bytes) =>
        new("image/png", null, new MemoryStream(bytes, false));
}

#endif
