namespace History.Commons.Ipc;

// Identifiers and the fixed shared-memory layout used by the Windows client and the background
// notification service for the post currently open in the client. The client writes the display
// section and reads the refresh section; the notification service does the opposite. Post ids are
// stored as UTF-16 in fixed buffers with an explicit length so the layout never varies.
public static class ClientPostDisplayProtocol
{
    public const string MemoryMappedFileName = @"Local\History.WindowsClientNotificationService.PostDisplay";

    public const string PostRefreshEventName = @"Local\History.WindowsClientNotificationService.PostRefresh";

    public const string PostReadEventName = @"Local\History.WindowsClientNotificationService.PostRead";

    public const int ProtocolVersion = 1;

    public const int MemorySize = 1024;

    public const int MaxPostIdLength = 128;

    public const int ProtocolVersionOffset = 0;

    // Display section: written by the client, read by the notification service.
    public const int DisplayedPlatformOffset = 4;
    public const int IsForegroundOffset = 8;
    public const int DisplayedPostIdLengthOffset = 12;
    public const int DisplayedPostIdOffset = 16;

    // Refresh section: written by the notification service, read by the client.
    public const int RefreshPlatformOffset = 272;
    public const int RefreshPostIdLengthOffset = 276;
    public const int RefreshPostIdOffset = 280;

    // Read section: written by the client, read by the notification service. Signals that a post's
    // notifications were read so the service can dismiss that post's system toasts.
    public const int PostReadPlatformOffset = 536;
    public const int PostReadPostIdLengthOffset = 540;
    public const int PostReadPostIdOffset = 544;
}
