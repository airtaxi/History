using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;

namespace History.WindowsClient.Messages;

// Close-block request that carries the originating window's XamlRoot, so only the owning window
// applies it, plus the optional reason shown when a close is attempted while the block is on.
public sealed class WindowCloseBlockRequestedMessage(XamlRoot xamlRoot, bool isCloseBlocked, string reason = null)
{
    public XamlRoot XamlRoot { get; } = xamlRoot;

    public bool IsCloseBlocked { get; } = isCloseBlocked;

    public string Reason { get; } = reason;

    public static void Send(XamlRoot xamlRoot, bool isCloseBlocked, string reason = null)
    {
        var message = new WindowCloseBlockRequestedMessage(xamlRoot, isCloseBlocked, reason);
        WeakReferenceMessenger.Default.Send(message);
    }
}
