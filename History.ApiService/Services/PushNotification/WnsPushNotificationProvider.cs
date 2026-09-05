using History.Commons;
using History.Commons.DataTypes;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System.Collections.Concurrent;
using System.Security;
using System.Text;
using System.Text.Json;

namespace History.ApiService.Services.PushNotification;

public class WnsPushNotificationProvider(IMongoDatabase database, WnsAccessTokenCache accessTokenCache, IOptions<WnsOptions> options, IHttpClientFactory httpClientFactory, ILogger<WnsPushNotificationProvider> logger) : IPushNotificationProvider
{
    public const string HttpClientName = "Wns";

    private const string ToastType = "wns/toast";
    private const int MaxConcurrentChannelSends = 20;
    private const int MaxTitleLength = 80;
    private const int MaxBodyLength = 100;

    // Protocol activation is handled by the Windows client's "history-app://toast" deep link.
    private const string ToastProtocolLaunchPrefix = "history-app://toast?";

    private readonly IMongoCollection<WnsChannel> _wnsChannelCollection = database.GetCollection<WnsChannel>("WnsChannels");
    private readonly SemaphoreSlim _sendSemaphore = new(MaxConcurrentChannelSends, MaxConcurrentChannelSends);

    public async Task<Result> SendAsync(IEnumerable<string> recipientUserIds, string title, string body, string imageUrl, Dictionary<string, string> data)
    {
        var recipientList = recipientUserIds?.ToList();
        if (recipientList == null || recipientList.Count == 0) return Result.Success();

        var filter = Builders<WnsChannel>.Filter.In(channel => channel.UserId, recipientList);
        var channels = await _wnsChannelCollection.Find(filter).ToListAsync();
        if (channels.Count == 0) return Result.Success();

        var payload = BuildToastPayload(title, body, imageUrl, data);
        if (payload == null) return Result.Success();

        var httpClient = httpClientFactory.CreateClient(HttpClientName);

        var accessToken = await accessTokenCache.GetAccessTokenAsync(RefreshAccessTokenAsync);
        if (accessToken == null)
        {
            logger.LogError("WNS access token could not be acquired; push notifications were skipped for {ChannelCount} channels.", channels.Count);
            return Result.Success();
        }

        var results = await SendToChannelsAsync(httpClient, channels, accessToken, payload);

        // Channels that WNS no longer recognizes (expired or invalid) must be removed from the database.
        var expiredChannels = results.Where(result => result.StatusCode is 404 or 410).Select(result => result.Channel).ToList();
        if (expiredChannels.Count > 0)
        {
            var expiredFilter = Builders<WnsChannel>.Filter.In(channel => channel.ChannelUri, expiredChannels.Select(channel => channel.ChannelUri));
            await _wnsChannelCollection.DeleteManyAsync(expiredFilter);
        }

        // A rejected access token is refreshed once, then the affected channels are retried a single time.
        var unauthorizedChannels = results.Where(result => result.StatusCode == 401).Select(result => result.Channel).ToList();
        if (unauthorizedChannels.Count > 0)
        {
            accessTokenCache.Invalidate();
            var refreshedAccessToken = await accessTokenCache.GetAccessTokenAsync(RefreshAccessTokenAsync);
            if (refreshedAccessToken != null) await SendToChannelsAsync(httpClient, unauthorizedChannels, refreshedAccessToken, payload);
        }

        return Result.Success();
    }

    private async Task<List<WnsSendResult>> SendToChannelsAsync(HttpClient httpClient, List<WnsChannel> channels, string accessToken, string payload)
    {
        var results = new ConcurrentBag<WnsSendResult>();
        var sendTasks = channels.Select(async channel =>
        {
            await _sendSemaphore.WaitAsync();
            try { results.Add(await SendToChannelAsync(httpClient, channel, accessToken, payload)); }
            finally { _sendSemaphore.Release(); }
        });
        await Task.WhenAll(sendTasks);

        return results.ToList();
    }

    private async Task<WnsSendResult> SendToChannelAsync(HttpClient httpClient, WnsChannel channel, string accessToken, string payload)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, channel.ChannelUri);
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {accessToken}");
        request.Headers.TryAddWithoutValidation("X-WNS-Type", ToastType);
        request.Content = new StringContent(payload, Encoding.UTF8, "text/xml");

        using var response = await httpClient.SendAsync(request);
        var statusCode = (int)response.StatusCode;

        if (statusCode != 200)
        {
            var errorDescription = response.Headers.TryGetValues("X-WNS-Error-Description", out var errorValues) ? string.Join(", ", errorValues) : null;
            var wnsStatus = response.Headers.TryGetValues("X-WNS-Status", out var statusValues) ? string.Join(", ", statusValues) : null;
            var messageId = response.Headers.TryGetValues("X-WNS-Msg-ID", out var messageIdValues) ? string.Join(", ", messageIdValues) : null;
            var debugTrace = response.Headers.TryGetValues("X-WNS-Debug-Trace", out var traceValues) ? string.Join(", ", traceValues) : null;
            logger.LogWarning("WNS notification delivery failed. StatusCode: {StatusCode}, Channel: {ChannelUri}, Error: {ErrorDescription}, WnsStatus: {WnsStatus}, MessageId: {MessageId}, DebugTrace: {DebugTrace}", statusCode, channel.ChannelUri, errorDescription, wnsStatus, messageId, debugTrace);
        }

        return new WnsSendResult(channel, statusCode);
    }

    private async Task<(string AccessToken, int ExpiresInSeconds)> RefreshAccessTokenAsync()
    {
        var requestBody = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = options.Value.ClientId,
            ["client_secret"] = options.Value.ClientSecret,
            ["scope"] = options.Value.Scope
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, options.Value.TokenEndpoint)
        {
            Content = new FormUrlEncodedContent(requestBody)
        };

        var httpClient = httpClientFactory.CreateClient(HttpClientName);
        using var response = await httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Failed to acquire WNS access token. StatusCode: {StatusCode}", (int)response.StatusCode);
            return (null, 0);
        }

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        var accessToken = root.GetProperty("access_token").GetString();
        var expiresInSeconds = root.TryGetProperty("expires_in", out var expiresProperty) && expiresProperty.TryGetInt32(out var expiresIn) ? expiresIn : 86400;
        return (accessToken, expiresInSeconds);
    }

    private static string BuildToastPayload(string title, string body, string imageUrl, Dictionary<string, string> data)
    {
        if (string.IsNullOrWhiteSpace(title)) return null;

        if (title.Length > MaxTitleLength) title = title[..MaxTitleLength];
        if (body != null && body.Length > MaxBodyLength) body = body[..MaxBodyLength];

        data.TryGetValue("notification_id", out var tag);
        data.TryGetValue("Type", out var group);

        var launchArguments = ToastProtocolLaunchPrefix + string.Join("&", data.Select(entry => $"{Uri.EscapeDataString(entry.Key)}={Uri.EscapeDataString(entry.Value)}"));

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
        if (Uri.IsWellFormedUriString(imageUrl, UriKind.Absolute)) builder.Append($"<image placement=\"inline\" src=\"{XmlEscape(imageUrl)}\"/>");
        builder.Append("</binding></visual></toast>");

        return builder.ToString();
    }

    private static string XmlEscape(string value) => SecurityElement.Escape(value) ?? string.Empty;

    private sealed record WnsSendResult(WnsChannel Channel, int StatusCode);
}