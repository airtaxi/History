using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;

namespace History.WindowsClient.Messages;

// Sticker create action that carries the originating window's XamlRoot, so pages only react
// to requests from their own window.
public sealed class StickerCreateRequestedMessage(XamlRoot xamlRoot)
{
    public XamlRoot XamlRoot { get; } = xamlRoot;

    public static void Send(XamlRoot xamlRoot)
    {
        var message = new StickerCreateRequestedMessage(xamlRoot);
        WeakReferenceMessenger.Default.Send(message);
    }
}
