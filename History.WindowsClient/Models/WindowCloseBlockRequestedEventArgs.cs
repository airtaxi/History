namespace History.WindowsClient.Models;

public sealed class WindowCloseBlockRequestedEventArgs(bool isCloseBlocked, string reason = null) : EventArgs
{
    public bool IsCloseBlocked { get; } = isCloseBlocked;

    public string Reason { get; } = reason;
}
