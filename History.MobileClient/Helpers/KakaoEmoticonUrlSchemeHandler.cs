#if IOS

using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using Foundation;
using WebKit;

namespace History.MobileClient.Helpers;

// Kakao CDN emoticon URLs are hotlink-protected (allow_referer=story.kakao.com)
// and WKWebView offers no way to intercept or modify request headers. Razor
// rewrites emoticon <img> src to kakaostory-emoticon:// (see
// KakaoEmoticonUriHelper), and this scheme handler performs the fetch with the
// Referer Kakao Story requires — mirroring the Android
// KakaoEmoticonWebViewClient interception. Responses are cached in memory
// because scheme-handler responses bypass WKWebView's own HTTP cache.
public sealed class KakaoEmoticonUrlSchemeHandler : NSObject, IWKUrlSchemeHandler
{
    private const int CacheEntryLimit = 300;

    private static readonly HttpClient s_httpClient = new();
    private static readonly ConcurrentDictionary<string, byte[]> s_cache = new();

    // WKWebViewConfiguration raises an exception when the same scheme is
    // registered twice, and the webview handler can connect more than once.
    public static void EnsureRegistered(WKWebViewConfiguration configuration)
    {
        if (configuration.GetUrlSchemeHandler(KakaoEmoticonUriHelper.DisplayScheme) != null) return;

        configuration.SetUrlSchemeHandler(new KakaoEmoticonUrlSchemeHandler(), KakaoEmoticonUriHelper.DisplayScheme);
    }

    public void StartUrlSchemeTask(WKWebView webView, IWKUrlSchemeTask urlSchemeTask)
    {
        var request = urlSchemeTask.Request;
        var rawUri = GetRawUri(request == null ? null : request.Url);
        if (rawUri == null)
        {
            // NSURLErrorBadURL
            urlSchemeTask.DidFailWithError(new NSError(NSError.NSUrlErrorDomain, -1000));
            return;
        }

        if (s_cache.TryGetValue(rawUri, out var cachedBytes))
        {
            Respond(urlSchemeTask, request.Url, cachedBytes);
            return;
        }

        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Get, rawUri);
            message.Headers.Referrer = new Uri(KakaoEmoticonUriHelper.KakaoStoryReferer);

            // The synchronous HttpClient.Send overload is not supported by iOS's
            // handler (PlatformNotSupportedException). Block on the async call
            // instead — StartUrlSchemeTask runs on WKWebView's own serial queue
            // without a SynchronizationContext, so no deadlock is possible.
            using var response = s_httpClient.SendAsync(message).ConfigureAwait(false).GetAwaiter().GetResult();

            if (response.StatusCode != HttpStatusCode.OK)
            {
                // NSURLErrorBadServerResponse
                urlSchemeTask.DidFailWithError(new NSError(NSError.NSUrlErrorDomain, -1011));
                return;
            }

            using var responseStream = response.Content.ReadAsStream();
            using var memoryStream = new MemoryStream();
            responseStream.CopyTo(memoryStream);

            var bytes = memoryStream.ToArray();
            if (s_cache.Count >= CacheEntryLimit) s_cache.Clear();
            s_cache[rawUri] = bytes;

            Respond(urlSchemeTask, request.Url, bytes);
        }
        catch
        {
            // Fetching with the Referer failed; fail this image load (the page
            // itself must survive).
            // NSURLErrorCannotLoadFromNetwork
            urlSchemeTask.DidFailWithError(new NSError(NSError.NSUrlErrorDomain, -2000));
        }
    }

    public void StopUrlSchemeTask(WKWebView webView, IWKUrlSchemeTask urlSchemeTask)
    {
        // The fetch is synchronous, so a stop cannot cancel an in-flight request.
    }

    private static string GetRawUri(NSUrl displayUrl)
    {
        if (displayUrl == null) return null;

        var query = displayUrl.Query; // "?url=<encoded>"
        if (string.IsNullOrEmpty(query) || !query.StartsWith("?url=", StringComparison.Ordinal)) return null;

        return Uri.UnescapeDataString(query[5..]);
    }

    private static void Respond(IWKUrlSchemeTask urlSchemeTask, NSUrl displayUrl, byte[] bytes)
    {
        urlSchemeTask.DidReceiveResponse(new NSUrlResponse(displayUrl, "image/png", bytes.Length, null));
        urlSchemeTask.DidReceiveData(NSData.FromArray(bytes));
        urlSchemeTask.DidFinish();
    }
}

#endif
