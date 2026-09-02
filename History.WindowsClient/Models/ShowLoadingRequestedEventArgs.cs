namespace History.WindowsClient.Models;

public sealed class ShowLoadingRequestedEventArgs(string loadingMessage) : EventArgs
{
    public string LoadingMessage { get; } = loadingMessage;
}