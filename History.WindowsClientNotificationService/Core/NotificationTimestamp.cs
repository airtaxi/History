namespace History.WindowsClientNotificationService.Core;

// Notification timestamps arrive as UTC from the server but can deserialize with an unspecified
// kind, so they are normalized before age comparisons.
public static class NotificationTimestamp
{
    public static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
