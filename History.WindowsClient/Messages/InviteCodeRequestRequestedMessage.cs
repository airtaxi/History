using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;

namespace History.WindowsClient.Messages;

// Invite code request action that carries the originating window's XamlRoot, so pages only
// react to requests from their own window.
public sealed class InviteCodeRequestRequestedMessage(XamlRoot xamlRoot)
{
    public XamlRoot XamlRoot { get; } = xamlRoot;

    public static void Send(XamlRoot xamlRoot)
    {
        var message = new InviteCodeRequestRequestedMessage(xamlRoot);
        WeakReferenceMessenger.Default.Send(message);
    }
}
