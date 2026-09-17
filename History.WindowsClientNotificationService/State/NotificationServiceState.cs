namespace History.WindowsClientNotificationService.State;

// Persisted de-duplication state. The History list uses a baseline flag plus the ids already
// delivered, and the Kakao Story list mirrors the server-side poller's known-id window.
public sealed class NotificationServiceState
{
    public List<string> HistoryKnownNotificationIds { get; set; } = [];

    public bool IsHistoryInitialized { get; set; }

    public List<string> KakaoStoryKnownNotificationIds { get; set; } = [];

    public bool IsKakaoStoryInitialized { get; set; }
}
