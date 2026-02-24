using History.Commons;
using History.Commons.DataTypes;
using History.Commons.Enums;

namespace History.ApiService.Services.Interfaces;

public interface INotificationService
{
    public Task<Result<List<Notification>>> GetNotificationsAsync(string userId, string fromNotificationId = null, int limit = 30);

    public Task<Result> RegisterFirebaseTokenAsync(string userId, string firebaseToken);

    public Task<Result<List<string>>> GetFirebaseTokensFromUserIdAsync(string userId);
    public Task<Result<List<string>>> GetFirebaseTokensFromUserIdsAsync(IEnumerable<string> userIds);

    public Task<Result> DeleteFirebaseTokensAsync(IEnumerable<string> firebaseTokens);

    public Task<Result> DeleteNotificationsAsync(string filterKey, string filterValue, NotificationType? type = null);
    public Task<Result> DeleteNotificationsAsync(string filterKey, IEnumerable<string> filterValues, NotificationType? type = null);

    public Task<Result> SendNotificationsAsync(NotificationType type, string associatedId);

    public Task<Result> SendFirebaseNotificationAsync(IEnumerable<string> recipientUserIds, string title, string body, string imageUrl, Dictionary<string, string> data);

    public Task<Result> HandleWithdrawAsync(string userId);

    public Task<Result> MarkNotificationsAsReadAsync(string userId, IEnumerable<string> notificationIds);

    public Task<Result> MarkAllNotificationsAsReadAsync(string userId);

    public Task<Result> MarkNotificationsByDataAsReadAsync(string userId, string dataKey, string dataValue, NotificationType? type = null);

    /// <summary>
    /// Returns the set of post IDs that have unread notifications for the given user.
    /// </summary>
    public Task<Result<HashSet<string>>> GetPostIdsWithUnreadNotificationsAsync(string userId, IEnumerable<string> postIds);
}
