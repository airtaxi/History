using History.Commons.DataTypes.Contents;
using HtmlAgilityPack;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Runtime.Versioning;
using System.Text;
using System.Text.RegularExpressions;

namespace History.ApiService.Helpers;

public static class ExternalUrlHelper
{
    private const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36 Edg/147.0.0.0";

    // Windows: WinHttpHandler (native WinHTTP TLS stack -> different fingerprint, bypasses Cloudflare)
    // Linux: SocketsHttpHandler with custom cipher suites to change TLS fingerprint
    [SupportedOSPlatform("windows")]
    public static HttpMessageHandler CreateWindowsHandler() => new WinHttpHandler();

    [SupportedOSPlatform("linux")]
    public static HttpMessageHandler CreateLinuxHandler() => new SocketsHttpHandler
    {
        SslOptions = new SslClientAuthenticationOptions
        {
            CipherSuitesPolicy = new CipherSuitesPolicy([TlsCipherSuite.TLS_AES_128_GCM_SHA256, TlsCipherSuite.TLS_AES_256_GCM_SHA384, TlsCipherSuite.TLS_CHACHA20_POLY1305_SHA256, TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256, TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256, TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384, TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384, TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_CHACHA20_POLY1305_SHA256, TlsCipherSuite.TLS_ECDHE_RSA_WITH_CHACHA20_POLY1305_SHA256, ])
        }
    };

    public static HttpMessageHandler CreateHandler() =>
        OperatingSystem.IsWindows() ? CreateWindowsHandler() : OperatingSystem.IsLinux() ? CreateLinuxHandler() : new HttpClientHandler();

    public static async Task<bool> FillExternalUrlContentAsync(ExternalUrlContent content)
    {
        if (string.IsNullOrEmpty(content?.SourceUrl)) return false;

        try
        {
            // Create a fresh handler per request so the TLS fingerprint rotates and Cloudflare cannot cache/block by fingerprint
            using var handler = CreateHandler();
            using var client = new HttpClient(handler, disposeHandler: true);
            ConfigureBrowserHeaders(client);

            using var request = new HttpRequestMessage(HttpMethod.Get, content.SourceUrl);
            request.Headers.Add("Upgrade-Insecure-Requests", "1");
            request.Headers.Add("Sec-Ch-Ua", "\"Microsoft Edge\";v=\"147\", \"Chromium\";v=\"147\", \"Not?A_Brand\";v=\"24\"");
            request.Headers.Add("Sec-Ch-Ua-Mobile", "?0");
            request.Headers.Add("Sec-Ch-Ua-Platform", "\"Windows\"");
            request.Headers.Add("Sec-Fetch-Dest", "document");
            request.Headers.Add("Sec-Fetch-Mode", "navigate");
            request.Headers.Add("Sec-Fetch-Site", "none");
            request.Headers.Add("Sec-Fetch-User", "?1");

            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead);

            if (!response.IsSuccessStatusCode) return false;

            // Track the final URI after redirects
            var finalUri = response.RequestMessage.RequestUri;
            if (finalUri != null && !string.Equals(finalUri.ToString(), content.SourceUrl, StringComparison.OrdinalIgnoreCase)) content.SourceUrl = finalUri.ToString();

            // Read raw bytes so we can decode using the charset declared by the server or the html meta tag
            byte[] responseBytes = await response.Content.ReadAsByteArrayAsync();
            if (responseBytes.Length == 0) return false;

            // Determine encoding: first from Content-Type header, then from meta tags in the HTML
            Encoding encoding = null;

            // Try parse charset from Content-Type header
            var contentType = response.Content.Headers.ContentType?.CharSet;
            if (!string.IsNullOrEmpty(contentType))
            {
                try
                {
                    encoding = Encoding.GetEncoding(contentType.Trim());
                    Console.WriteLine($"Charset from header: {encoding.WebName}");
                }
                catch { encoding = null; }
            }

            string htmlText = null;

            if (encoding == null)
            {
                // Decode as UTF8 to reliably search for meta charset declarations in the raw bytes
                var tentativeText = Encoding.UTF8.GetString(responseBytes);

                // Use HtmlAgilityPack on tentative text to find meta charset attributes
                var tentativeDoc = new HtmlDocument();
                tentativeDoc.LoadHtml(tentativeText);

                var metaCharset = tentativeDoc.DocumentNode.SelectSingleNode("//meta[@charset]")?.GetAttributeValue("charset", null);
                if (!string.IsNullOrEmpty(metaCharset))
                {
                    try
                    {
                        encoding = Encoding.GetEncoding(metaCharset.Trim());
                        Console.WriteLine($"metaCharset: {encoding.WebName}");
                    }
                    catch { encoding = null; }
                }

                if (encoding == null)
                {
                    var metaContent = tentativeDoc.DocumentNode.SelectSingleNode("//meta[translate(@http-equiv, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz')='content-type']")?.GetAttributeValue("content", null);
                    if (!string.IsNullOrEmpty(metaContent))
                    {
                        var metaMatch = Regex.Match(metaContent, @"charset\s*=\s*([^;,\r\n]+)", RegexOptions.IgnoreCase);
                        if (metaMatch.Success)
                        {
                            var charset = metaMatch.Groups[1].Value.Trim().Trim('"', '\'');
                            try
                            {
                                encoding = Encoding.GetEncoding(charset);
                                Console.WriteLine($"meta http-equiv charset: {encoding.WebName}");
                            }
                            catch (Exception exception)
                            {
                                Console.WriteLine("Exception while getting encoding from meta http-equiv: " + exception.Message);
                                encoding = null;
                            }
                        }
                    }
                }

                if (encoding == null)
                {
                    // No charset found in headers or meta tags; default to UTF-8
                    encoding = Encoding.UTF8;
                    Console.WriteLine("(Warning) Fallback to UTF-8");
                }
            }

            htmlText = encoding.GetString(responseBytes);
            Console.WriteLine($"Encoding: {encoding.WebName}");

            var doc = new HtmlDocument();
            doc.LoadHtml(htmlText);

            content.Title = HtmlDecode(GetMetaTagContent(doc, "og:title")
                         ?? GetDocumentTitle(doc)
                         ?? "제목 없음");

            content.Description = HtmlDecode(GetMetaTagContent(doc, "og:description") ?? GetMetaTagContent(doc, "description") ?? content.SourceUrl);

            content.ThumbnailImageUrl = HtmlDecode(GetMetaTagContent(doc, "og:image") ?? GetMetaTagContent(doc, "og:image:url") ?? GetMetaTagContent(doc, "og:image:secure_url") ?? GetMetaTagContent(doc, "twitter:image") ?? GetMetaTagContent(doc, "twitter:image:src") ?? GetLinkHref(doc, "image_src") ?? GetLinkHref(doc, "apple-touch-icon") ?? GetLinkHref(doc, "icon") ?? GetFirstImageUrl(doc, content.SourceUrl) ?? "");

            return true;
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Error while fetching URL metadata: {exception.Message}");
            return false;
        }
    }

    private static void ConfigureBrowserHeaders(HttpClient client)
    {
        client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xhtml+xml"));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml", 0.9));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("image/avif"));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("image/webp"));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("image/apng"));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*", 0.8));
        client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("ko-KR"));
        client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en-US", 0.9));
        client.DefaultRequestHeaders.Add("Connection", "keep-alive");
    }

    private static string GetMetaTagContent(HtmlDocument doc, string property) =>
        doc.DocumentNode.SelectSingleNode($"//meta[@property='{property}']")?.GetAttributeValue("content", null) ?? doc.DocumentNode.SelectSingleNode($"//meta[@name='{property}']")?.GetAttributeValue("content", null);

    private static string GetLinkHref(HtmlDocument doc, string rel) =>
        doc.DocumentNode.SelectSingleNode($"//link[translate(@rel, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz')='{rel}']")?.GetAttributeValue("href", null);

    private static string GetDocumentTitle(HtmlDocument doc) => doc.DocumentNode.SelectSingleNode("//title")?.InnerText?.Trim();

    private static string GetFirstImageUrl(HtmlDocument doc, string baseUrl)
    {
        var imgNode = doc.DocumentNode.SelectSingleNode("//img[@src]");
        if (imgNode != null)
        {
            var src = imgNode.GetAttributeValue("src", null);
            if (!string.IsNullOrEmpty(src))
            {
                if (Uri.IsWellFormedUriString(src, UriKind.Absolute)) return src;
                else
                {
                    try
                    {
                        var baseUri = new Uri(baseUrl);
                        var absoluteUri = new Uri(baseUri, src);
                        return absoluteUri.ToString();
                    }
                    catch { return null; }
                }
            }
        }
        return null;
    }

    private static string HtmlDecode(string value) => string.IsNullOrEmpty(value) ? value : WebUtility.HtmlDecode(value);
}