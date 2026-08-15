namespace History.MobileClient.Helpers;

// Shared constants and display-URI logic for Kakao Story emoticon images.
//
// The CDN (mk.kakaocdn.net) hotlink-protects emoticon URLs with
// allow_referer=story.kakao.com, and webview <img> loaders cannot send a
// custom Referer header:
// - Android: KakaoEmoticonWebViewClient intercepts the raw https request and
//   re-issues it with the Referer.
// - iOS: WKWebView has no interception API, so the <img> src is rewritten to
//   a custom scheme that KakaoEmoticonUrlSchemeHandler handles.
public static class KakaoEmoticonUriHelper
{
    public const string EmoticonUrlPrefix = "https://mk.kakaocdn.net/dna/emoticons";
    public const string KakaoStoryReferer = "https://story.kakao.com/";
    public const string DisplayScheme = "kakaostory-emoticon";

#if IOS
    // The signed URL lives in the query string because its signature is
    // case-sensitive and URL hosts may get case-normalized.
    public static string GetDisplayUri(string rawUri) => rawUri != null && rawUri.StartsWith(EmoticonUrlPrefix, StringComparison.Ordinal)
        ? $"{DisplayScheme}://emoticon?url={Uri.EscapeDataString(rawUri)}"
        : rawUri;
#else
    public static string GetDisplayUri(string rawUri) => rawUri;
#endif
}
