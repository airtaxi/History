using System.Collections.Concurrent;
using System.Text.Json;
using History.ApiService.DataTypes;
using History.ApiService.Helpers;
using History.ApiService.Services.Interfaces;
using History.Commons.DataTypes;
using History.Commons.Enums;
using MongoDB.Driver;

namespace History.ApiService.Services;

/// <summary>
/// Polls Kakao Story notifications for users who uploaded their KAuth tokens,
/// proxying requests through a Cloudflare Worker so Kakao sees Cloudflare IPs.
/// Tokens are kept in memory only (never persisted); the per-user known
/// notification id array used for deduplication is persisted in MongoDB so a
/// poll window overflow cannot re-send an already-delivered notification.
/// New notifications are delivered via FCM push. When a request returns 401 the
/// session is dropped; the next token upload (e.g. from the client's periodic
/// job) re-registers the session.
/// </summary>
public class KakaoStoryPollingService(ILogger<KakaoStoryPollingService> logger, IConfiguration configuration, KakaoStoryWorkerClient workerClient, INotificationService notificationService, IMongoDatabase database) : IHostedService, IDisposable
{
    private const string NotificationsUrl = "https://story.kakao.com/a/notifications";
    private const int MaxKnownNotificationIds = 100;

    private readonly ConcurrentDictionary<string, KakaoStorySession> _sessions = new();
    private readonly IMongoCollection<KakaoStoryNotificationState> _stateCollection = database.GetCollection<KakaoStoryNotificationState>("KakaoStoryNotificationStates");
    private Timer _timer;
    private bool _isPolling;

    public void RegisterSession(string userId, string idToken, bool isFavoriteFriendNotificationEnabled, bool isEmotionNotificationEnabled)
    {
        _sessions[userId] = new KakaoStorySession
        {
            IdToken = idToken,
            IsFavoriteFriendNotificationEnabled = isFavoriteFriendNotificationEnabled,
            IsEmotionNotificationEnabled = isEmotionNotificationEnabled
        };
        logger.LogInformation("Kakao Story session registered for user {UserId}. Active sessions: {Count}", userId, _sessions.Count);
    }

    public void RemoveSession(string userId)
    {
        if (_sessions.TryRemove(userId, out _))
        {
            logger.LogInformation("Kakao Story session removed for user {UserId}. Active sessions: {Count}", userId, _sessions.Count);
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var intervalSeconds = configuration.GetValue("KakaoStoryPolling:PollIntervalSeconds", 60);
        _timer = new Timer(ExecutePoll, null, TimeSpan.Zero, TimeSpan.FromSeconds(intervalSeconds));
        logger.LogInformation("Kakao Story polling service started. Interval: {Interval}s, Worker configured: {Configured}", intervalSeconds, workerClient.IsConfigured);
        return Task.CompletedTask;
    }

    private async void ExecutePoll(object state)
    {
        if (_isPolling) return;
        if (!workerClient.IsConfigured) return;
        if (_sessions.IsEmpty) return;

        _isPolling = true;
        try
        {
            var batchSize = configuration.GetValue("KakaoStoryPolling:BatchSize", 10);
            var sessions = _sessions.ToArray();
            foreach (var chunk in sessions.Chunk(batchSize)) await PollBatchAsync(chunk);
        }
        catch (Exception exception) { logger.LogError(exception, "Kakao Story polling failed: {Message}", exception.Message); }
        finally { _isPolling = false; }
    }

    private async Task PollBatchAsync(KeyValuePair<string, KakaoStorySession>[] sessions)
    {
        var request = new KakaoStoryWorkerBatchRequest
        {
            Requests = [.. sessions.Select(session => new KakaoStoryWorkerRequest
            {
                Url = NotificationsUrl,
                IdToken = session.Value.IdToken
            })]
        };

        KakaoStoryWorkerBatchResponse response;
        try { response = await workerClient.PostBatchAsync(request); }
        catch (Exception exception)
        {
            logger.LogError(exception, "Kakao Story worker batch request failed: {Message}", exception.Message);
            return;
        }

        for (int i = 0; i < response.Responses.Count; i++)
        {
            var (userId, session) = sessions[i];
            var workerResponse = response.Responses[i];
            await HandleWorkerResponseAsync(userId, session, workerResponse);
        }
    }

    private async Task HandleWorkerResponseAsync(string userId, KakaoStorySession session, KakaoStoryWorkerResponse workerResponse)
    {
        if (workerResponse.Status == 401)
        {
            // The id token expired: drop the session. The client re-uploads a
            // fresh token on its periodic job, so no re-login push is needed.
            RemoveSession(userId);
            return;
        }
        else if (workerResponse.Status != 200) return;

        List<KakaoStoryNotification> notifications;
        try { notifications = JsonSerializer.Deserialize<List<KakaoStoryNotification>>(workerResponse.Body) ?? []; }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to parse Kakao Story notifications for user {UserId}", userId);
            return;
        }

        var state = await _stateCollection.Find(s => s.UserId == userId).FirstOrDefaultAsync();
        if (state == null)
        {
            // Fresh start: record every known id so only notifications arriving
            // after this point are delivered.
            await SaveStateAsync(userId, [.. notifications.Select(notification => notification.Id).Where(id => id != null)]);
            return;
        }

        var knownIds = new HashSet<string>(state.KnownNotificationIds ?? []);
        var newNotifications = notifications
            .Where(notification => notification.IsNew && notification.Id != null && !knownIds.Contains(notification.Id))
            .Where(notification => !IsFavoriteFriendNotification(notification) || session.IsFavoriteFriendNotificationEnabled)
            .Where(notification => !IsEmotionNotification(notification) || session.IsEmotionNotificationEnabled)
            .OrderBy(notification => notification.CreatedAt)
            .ToList();

        foreach (var notification in newNotifications) await SendNotificationAsync(userId, notification);

        // Record every fetched id (the API list is newest-first) and trim to the
        // newest 100 so the array cannot grow unbounded.
        var mergedIds = notifications.Select(notification => notification.Id).Where(id => id != null).Concat(knownIds).Distinct().Take(MaxKnownNotificationIds).ToList();
        await SaveStateAsync(userId, mergedIds);
    }

    private async Task SaveStateAsync(string userId, List<string> knownNotificationIds)
    {
        var filter = Builders<KakaoStoryNotificationState>.Filter.Eq(s => s.UserId, userId);
        var update = Builders<KakaoStoryNotificationState>.Update
            .SetOnInsert(s => s.Id, Guid.NewGuid().ToString("N"))
            .Set(s => s.KnownNotificationIds, knownNotificationIds)
            .Set(s => s.UpdatedAt, DateTime.UtcNow);
        await _stateCollection.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true });
    }

    /// <summary>
    /// Mirrors the client's KakaoStoryNotificationPoller: a favorite friend
    /// notification carries a decorator whose text starts with "관심친구".
    /// </summary>
    private static bool IsFavoriteFriendNotification(KakaoStoryNotification notification) => notification.Decorators is { Count: > 0 } && notification.Decorators[0].Text?.StartsWith("관심친구") == true;

    /// <summary>
    /// Mirrors the client's KakaoStoryNotificationPoller: an emotion notification
    /// carries a non-null emotion field.
    /// </summary>
    private static bool IsEmotionNotification(KakaoStoryNotification notification) => notification.Emotion != null;

    private async Task SendNotificationAsync(string userId, KakaoStoryNotification notification)
    {
        var title = notification.Message ?? "카카오스토리 알림";
        var body = notification.Content ?? string.Empty;
        if (body.Length > 100) body = body[..100] + "...";

        var data = new Dictionary<string, string>
        {
            { "Type", "KakaoStory" },
            { "NotificationId", notification.Id }
        };
        if (notification.Scheme != null) data["Scheme"] = notification.Scheme;

        await notificationService.SendFirebaseNotificationAsync([userId], title, body, notification.ThumbnailUrl, data);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
        GC.SuppressFinalize(this);
    }

    private class KakaoStorySession
    {
        public string IdToken { get; set; }

        public bool IsFavoriteFriendNotificationEnabled { get; set; }

        public bool IsEmotionNotificationEnabled { get; set; }
    }
}
