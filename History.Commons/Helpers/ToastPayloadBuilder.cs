using System.Security;
using System.Text;

namespace History.Commons.Helpers;

// Builds the protocol-activation toast payload shared by the server-side WNS provider and the
// Windows background notification service. Both send the same XML so a locally generated
// notification behaves identically to a pushed one: clicking it opens the app through the
// "history-app://toast?...?" deep link handled by the client, and the title/body limits plus the
// tag/group collapse keys match the server's payload. The app logo is optional and supplied only
// by the local service, which can reference an asset of the installed package.
public static class ToastPayloadBuilder
{
    private const string ProtocolLaunchPrefix = "history-app://toast?";
    private const int MaxTitleLength = 80;
    private const int MaxBodyLength = 100;

    public static string Build(string title, string body, string imageUrl, IReadOnlyDictionary<string, string> data, string appLogoSource = null)
    {
        if (string.IsNullOrWhiteSpace(title)) return null;

        if (title.Length > MaxTitleLength) title = title[..MaxTitleLength];
        if (body != null && body.Length > MaxBodyLength) body = body[..MaxBodyLength];

        string tag = null;
        string group = null;
        if (data != null)
        {
            data.TryGetValue("notification_id", out tag);
            data.TryGetValue("Type", out group);
        }

        var launchArguments = ProtocolLaunchPrefix + (data == null ? string.Empty : string.Join("&", data.Select(entry => $"{Uri.EscapeDataString(entry.Key)}={Uri.EscapeDataString(entry.Value)}")));

        var builder = new StringBuilder();
        builder.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        builder.Append("<toast activationType=\"protocol\"");
        if (launchArguments.Length > 0) builder.Append($" launch=\"{XmlEscape(launchArguments)}\"");
        if (!string.IsNullOrEmpty(tag)) builder.Append($" tag=\"{XmlEscape(tag)}\"");
        if (!string.IsNullOrEmpty(group)) builder.Append($" group=\"{XmlEscape(group)}\"");
        builder.Append(">");
        builder.Append("<visual><binding template=\"ToastGeneric\">");
        builder.Append($"<text>{XmlEscape(title)}</text>");
        if (!string.IsNullOrEmpty(body)) builder.Append($"<text>{XmlEscape(body)}</text>");
        if (Uri.IsWellFormedUriString(appLogoSource, UriKind.Absolute)) builder.Append($"<image placement=\"appLogoOverride\" src=\"{XmlEscape(appLogoSource)}\"/>");
        if (Uri.IsWellFormedUriString(imageUrl, UriKind.Absolute)) builder.Append($"<image placement=\"inline\" src=\"{XmlEscape(imageUrl)}\"/>");
        builder.Append("</binding></visual></toast>");

        return builder.ToString();
    }

    private static string XmlEscape(string value) => SecurityElement.Escape(value) ?? string.Empty;
}
