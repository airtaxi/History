using FirebaseAdmin.Messaging;
using History.Commons;
using History.Commons.DataTypes;
using MongoDB.Driver;

namespace History.ApiService.Services.PushNotification;

public class FirebasePushNotificationProvider(IMongoDatabase database) : IPushNotificationProvider
{
    private const string AndroidChannelId = "com.airtaxi.history.push";

    private readonly IMongoCollection<FirebaseToken> _firebaseTokenCollection = database.GetCollection<FirebaseToken>("FirebaseTokens");

    public async Task<Result> SendAsync(IEnumerable<string> recipientUserIds, string title, string body, string imageUrl, Dictionary<string, string> data)
    {
        if (!Uri.IsWellFormedUriString(imageUrl, UriKind.Absolute)) imageUrl = null;

        var filter = Builders<FirebaseToken>.Filter.In(token => token.UserId, recipientUserIds);
        var firebaseTokens = await _firebaseTokenCollection.Find(filter).ToListAsync();
        var tokens = firebaseTokens.Select(token => token.Token).ToList();

        if (tokens.Count == 0) return Result.Success();

        data.TryGetValue("notification_id", out var collapseKey);

        var message = new MulticastMessage
        {
            Tokens = tokens,
            Notification = new()
            {
                Title = title,
                Body = body,
                ImageUrl = imageUrl,
            },
            Data = data,
            Android = new AndroidConfig
            {
                Priority = Priority.High,
                Notification = new AndroidNotification
                {
                    ChannelId = AndroidChannelId,
                    ImageUrl = imageUrl,
                    NotificationCount = 1,
                    EventTimestamp = DateTime.UtcNow
                },
            }
        };

        if (collapseKey != null)
        {
            message.Android.Notification.Tag = collapseKey;
            if (message.Apns != null) message.Apns = new ApnsConfig { Headers = new Dictionary<string, string> { { "apns-collapse-id", collapseKey } } };
        }

        var response = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(message);

        var expiredTokens = new List<string>();
        for (var index = 0; index < response.Responses.Count; index++)
        {
            var result = response.Responses[index];
            if (result.Exception != null)
            {
                var errorCode = result.Exception.MessagingErrorCode;

                if (errorCode == MessagingErrorCode.Unregistered || errorCode == MessagingErrorCode.InvalidArgument) expiredTokens.Add(tokens[index]);
            }
        }

        if (expiredTokens.Count > 0)
        {
            var expiredFilter = Builders<FirebaseToken>.Filter.In(token => token.Token, expiredTokens);
            await _firebaseTokenCollection.DeleteManyAsync(expiredFilter);
        }

        return Result.Success();
    }
}