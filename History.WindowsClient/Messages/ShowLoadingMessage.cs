using CommunityToolkit.Mvvm.Messaging;
using History.WindowsClient.Models;
using Microsoft.UI.Xaml;

namespace History.WindowsClient.Messages;

// Show request that carries the originating host's XamlRoot, so only the owning window
// reacts to it.
public sealed class ShowLoadingMessage(XamlRoot xamlRoot, string loadingMessage)
{
    public XamlRoot XamlRoot { get; } = xamlRoot;

    public string LoadingMessage { get; } = loadingMessage;

    public static void Send(XamlRoot xamlRoot, ShowLoadingRequestedEventArgs args)
    {
        var message = new ShowLoadingMessage(xamlRoot, args.LoadingMessage);
        WeakReferenceMessenger.Default.Send(message);
    }
}