using History.Commons;
using History.Commons.DataTypes;
using History.Commons.Enums;

namespace History.ApiService.Services.Interfaces;

public interface INotificationService
{
    public Task<Result<List<Notification>>> GetNotificationsAsync(string userId, string fromNotificationId = null, int limit = 30);

    public Task<Result> RegisterFirebaseTokenAsync(string userId, string firebaseToken);

    public Task<Result> RegisterWnsChannelAsync(string userId, string channelUri);
    public Task<Result> DeleteWnsChannelsAsync(IEnumerable<string> channelUris);

    public Task<Result> DeleteNotificationsAsync(string filterKey, string filterValue, NotificationType? type = null);
    public Task<Result> DeleteNotificationsAsync(string filterKey, IEnumerable<string> filterValues, NotificationType? type = null);

    public Task<Result> SendNotificationsAsync(NotificationType type, string associatedId);

    public Task<Result> SendPushNotificationsAsync(IEnumerable<string> recipientUserIds, string title, string body, string imageUrl, Dictionary<string, string> data);

    public Task<Result> HandleWithdrawAsync(string userId);

    public Task<Result> MarkNotificationsAsReadAsync(string userId, IEnumerable<string> notificationIds);

    public Task<Result> MarkAllNotificationsAsReadAsync(string userId);

    public Task<Result> MarkNotificationsByDataAsReadAsync(string userId, string dataKey, string dataValue, NotificationType? type = null);

    public Task<Result> MarkNotificationsByTypeAsReadAsync(string userId, NotificationType type);

    /// <summary>
    /// Returns the set of post IDs that have unread notifications for the given user.
    /// </summary>
    public Task<Result<HashSet<string>>> GetPostIdsWithUnreadNotificationsAsync(string userId, IEnumerable<string> postIds);

    /// <summary>
    /// Removes a user from existing notifications of a post, deleting notifications whose recipients become empty.
    /// </summary>
    /// <param name="postId">The ID of the post whose notifications are being cleaned up.</param>
    /// <param name="userId">The ID of the user to remove from the notifications.</param>
    /// <returns>A task that represents the asynchronous operation, containing the result of the removal.</returns>
    public Task<Result> RemoveUserNotificationsForPostAsync(string postId, string userId);
}
