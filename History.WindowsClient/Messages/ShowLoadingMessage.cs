using CommunityToolkit.Mvvm.Messaging;
using History.WindowsClient.Models;

namespace History.WindowsClient.Messages;

public sealed class ShowLoadingMessage(string loadingMessage)
{
    public string LoadingMessage { get; } = loadingMessage;

    public static void Send(ShowLoadingRequestedEventArgs args)
    {
        var message = new ShowLoadingMessage(args.LoadingMessage);
        WeakReferenceMessenger.Default.Send(message);
    }
}