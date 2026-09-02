namespace History.WindowsClient.Models;

public sealed class LoadingStateRequestedEventArgs(string loadingMessage, Func<Task> action) : EventArgs
{
    public string LoadingMessage { get; } = loadingMessage;

    public Func<Task> Action { get; } = action;

    public Task ResultTask { get; set; }
}