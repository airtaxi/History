using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;

namespace History.WindowsClient.Messages;

// Refresh request that carries the originating window's XamlRoot, so pages only react to
// requests from their own window.
public sealed class RefreshRequestedMessage(XamlRoot xamlRoot)
{
    public XamlRoot XamlRoot { get; } = xamlRoot;

    public static void Send(XamlRoot xamlRoot)
    {
        var message = new RefreshRequestedMessage(xamlRoot);
        WeakReferenceMessenger.Default.Send(message);
    }
}
