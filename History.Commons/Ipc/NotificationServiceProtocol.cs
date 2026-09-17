namespace History.Commons.Ipc;

// Identifiers shared by the Windows client and the background notification service it ships with.
// The client uses them to start and stop the service, and both sides use the stop event so the
// service leaves its poll loop instead of being terminated.
public static class NotificationServiceProtocol
{
    public const string TaskId = "HistoryNotificationService";

    public const string ExecutableName = "History.WindowsClientNotificationService.exe";

    public const string ProcessName = "History.WindowsClientNotificationService";

    public const string StopEventName = @"Local\History.WindowsClientNotificationService.Stop";
}
