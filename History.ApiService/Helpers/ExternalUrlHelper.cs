using History.Commons.DataTypes.Contents;
using HtmlAgilityPack;
using RestSharp;
using System.Net;

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

            if (!response.IsSuccessful || string.IsNullOrEmpty(response.Content))
                return false;

            if (response.ResponseUri != null
                && !string.Equals(response.ResponseUri.ToString(), content.SourceUrl, StringComparison.OrdinalIgnoreCase))
                content.SourceUrl = response.ResponseUri.ToString();

            var doc = new HtmlDocument();
            doc.LoadHtml(response.Content);

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