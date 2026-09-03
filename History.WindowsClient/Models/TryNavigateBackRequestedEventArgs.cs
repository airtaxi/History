namespace History.WindowsClient.Models;

public sealed class TryNavigateBackRequestedEventArgs : EventArgs
{
    public Task<bool> ResultTask { get; set; }
}
