using History.Commons;

namespace History.ApiService.Services.PushNotification;

public interface IPushNotificationProvider
{
    public Task<Result> SendAsync(IEnumerable<string> recipientUserIds, string title, string body, string imageUrl, Dictionary<string, string> data);
}