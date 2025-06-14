using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

#if ANDROID
using Android.Webkit;
using WebView = Microsoft.Maui.Controls.WebView;
#elif IOS
using Foundation;
using WebKit;
#endif

namespace History.MobileClient.Helpers;

public static class WebViewCookieHelper
{
    public static Task<List<Cookie>> GetCookieListAsync(WebView webView, string url)
    {
#if ANDROID
        return GetCookieListAndroidAsync(webView, url);
#elif IOS
        return GetCookieListiOSAsync(webView, url);
#else
        return Task.FromResult(new List<Cookie>());
#endif
    }

#if ANDROID
    private static Task<List<Cookie>> GetCookieListAndroidAsync(WebView webView, string url)
    {
        var cookies = new List<Cookie>();
        var cookieManager = CookieManager.Instance;
        var cookieString = cookieManager?.GetCookie(url);

        if (!string.IsNullOrEmpty(cookieString))
        {
            var uri = new Uri(url);
            var cookiePairs = cookieString.Split(';');

            foreach (var cookiePair in cookiePairs)
            {
                var parts = cookiePair.Trim().Split(['='], 2);
                if (parts.Length == 2)
                {
                    var cookie = new Cookie(parts[0].Trim(), parts[1].Trim())
                    {
                        Domain = uri.Host
                    };
                    cookies.Add(cookie);
                }
            }
        }
        return Task.FromResult(cookies);
    }
#endif

#if IOS
    private static Task<List<Cookie>> GetCookieListiOSAsync(WebView webView, string url)
    {
        var tcs = new TaskCompletionSource<List<Cookie>>();

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                var cookies = new List<Cookie>();

                if (webView?.Handler?.PlatformView is WKWebView wkWebView)
                {
                    var cookieStore = wkWebView.Configuration.WebsiteDataStore.HttpCookieStore;
                    var allCookies = await cookieStore.GetAllCookiesAsync();
                    var targetUri = new Uri(url);

                    foreach (NSHttpCookie nsCookie in allCookies)
                    {
                        if (nsCookie.Domain == targetUri.Host ||
                            targetUri.Host.EndsWith(nsCookie.Domain) ||
                            (nsCookie.Domain.StartsWith(".") && targetUri.Host.EndsWith(nsCookie.Domain.Substring(1))))
                        {
                            var cookie = new Cookie(nsCookie.Name, nsCookie.Value)
                            {
                                Domain = nsCookie.Domain,
                                Path = nsCookie.Path,
                                Secure = nsCookie.IsSecure,
                                HttpOnly = nsCookie.IsHttpOnly
                            };

                            if (nsCookie.ExpiresDate != null)
                            {
                                var expiresDate = new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                                    .AddSeconds(nsCookie.ExpiresDate.SecondsSinceReferenceDate);
                                cookie.Expires = expiresDate;
                            }

                            cookies.Add(cookie);
                        }
                    }
                }

                tcs.SetResult(cookies);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });

        return tcs.Task;
    }
#endif
}
