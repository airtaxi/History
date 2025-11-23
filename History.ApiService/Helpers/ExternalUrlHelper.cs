using History.Commons.DataTypes.Contents;
using HtmlAgilityPack;
using RestSharp;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace History.ApiService.Helpers;

public static class ExternalUrlHelper
{
    public static async Task<bool> FillExternalUrlContentAsync(ExternalUrlContent content)
    {
        if (string.IsNullOrEmpty(content?.SourceUrl))
            return false;

        try
        {
            var client = new RestClient();
            var request = new RestRequest(content.SourceUrl, Method.Get);

            var response = await client.ExecuteAsync(request);

            if (!response.IsSuccessful) return false;

            if (response.ResponseUri != null
                && !string.Equals(response.ResponseUri.ToString(), content.SourceUrl, StringComparison.OrdinalIgnoreCase))
                content.SourceUrl = response.ResponseUri.ToString();

            // Prefer raw bytes so we can decode using the charset declared by the server or the html meta tag
            byte[] responseBytes = response.RawBytes ?? (response.Content != null ? Encoding.UTF8.GetBytes(response.Content) : null);
            if (responseBytes == null || responseBytes.Length == 0) return false;

            // Determine encoding: first from Content-Type header, then from meta tags in the HTML
            Encoding encoding = null;

            // Try parse charset from Content-Type header
            var contentType = response.ContentType;
            if (!string.IsNullOrEmpty(contentType))
            {
                var headerMatch = Regex.Match(contentType, @"charset\s*=\s*([^;,\r\n]+)", RegexOptions.IgnoreCase);
                if (headerMatch.Success)
                {
                    var charset = headerMatch.Groups[1].Value.Trim().Trim('"', '\'');
                    Console.WriteLine($"Charset: {encoding.WebName}");
                    try { encoding = Encoding.GetEncoding(charset); } catch { encoding = null; }
                }
            }

            string htmlText = null;

            if (encoding == null)
            {
                // Decode as UTF8 to reliably search for meta charset declarations in the raw bytes
                var utf8 = Encoding.UTF8;
                var tentativeText = utf8.GetString(responseBytes);

                // Use HtmlAgilityPack on tentative text to find meta charset attributes
                var tentativeDoc = new HtmlDocument();
                tentativeDoc.LoadHtml(tentativeText);

                Console.WriteLine($"text: {tentativeText}");

                var metaCharset = tentativeDoc.DocumentNode.SelectSingleNode("//meta[@charset]")?.GetAttributeValue("charset", null);
                if (!string.IsNullOrEmpty(metaCharset))
                {
                    Console.WriteLine($"metaCharset: {encoding.WebName}");
                    try { encoding = Encoding.GetEncoding(metaCharset.Trim()); } catch { encoding = null; }
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
                            try { encoding = Encoding.GetEncoding(charset); } catch { encoding = null; }
                        }
                    }
                }

                if (encoding == null)
                {
                    // No charset found in headers or meta tags; default to UTF-8
                    encoding = Encoding.UTF8;
                }

                Console.WriteLine($"Encoding: {encoding.WebName}");
                htmlText = encoding.GetString(responseBytes);
            }
            else
            {
                // We have encoding from header
                Console.WriteLine($"(Fallback) Encoding: {encoding.WebName}");
                htmlText = encoding.GetString(responseBytes);
            }

            var doc = new HtmlDocument();
            doc.LoadHtml(htmlText);

            content.Title = HtmlDecode(GetMetaTagContent(doc, "og:title")
                         ?? GetDocumentTitle(doc)
                         ?? "제목 없음");

            content.Description = HtmlDecode(GetMetaTagContent(doc, "og:description")
                               ?? GetMetaTagContent(doc, "description")
                               ?? content.SourceUrl);

            content.ThumbnailImageUrl = HtmlDecode(GetMetaTagContent(doc, "og:image")
                                ?? GetFirstImageUrl(doc, content.SourceUrl)
                                ?? "");

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while fetching URL metadata: {ex.Message}");
            return false;
        }
    }

    private static string GetMetaTagContent(HtmlDocument doc, string property) =>
        doc.DocumentNode.SelectSingleNode($"//meta[@property='{property}']")?.GetAttributeValue("content", null) ??
        doc.DocumentNode.SelectSingleNode($"//meta[@name='{property}']")?.GetAttributeValue("content", null);

    private static string GetDocumentTitle(HtmlDocument doc) => doc.DocumentNode.SelectSingleNode("//title")?.InnerText?.Trim();

    private static string GetFirstImageUrl(HtmlDocument doc, string baseUrl)
    {
        var imgNode = doc.DocumentNode.SelectSingleNode("//img[@src]");
        if (imgNode != null)
        {
            var src = imgNode.GetAttributeValue("src", null);
            if (!string.IsNullOrEmpty(src))
            {
                if (Uri.IsWellFormedUriString(src, UriKind.Absolute))
                    return src;
                else
                {
                    try
                    {
                        var baseUri = new Uri(baseUrl);
                        var absoluteUri = new Uri(baseUri, src);
                        return absoluteUri.ToString();
                    }
                    catch
                    {
                        return null;
                    }
                }
            }
        }
        return null;
    }

    private static string HtmlDecode(string value) => string.IsNullOrEmpty(value) ? value : WebUtility.HtmlDecode(value);
}