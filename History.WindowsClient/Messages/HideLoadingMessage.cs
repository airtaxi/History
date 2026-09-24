using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;

namespace History.WindowsClient.Messages;

// Hide request that carries the originating host's XamlRoot, so only the owning window
// reacts to it.
public sealed class HideLoadingMessage(XamlRoot xamlRoot)
{
    public XamlRoot XamlRoot { get; } = xamlRoot;

    public static void Send(XamlRoot xamlRoot) => WeakReferenceMessenger.Default.Send(new HideLoadingMessage(xamlRoot));
}