using CommunityToolkit.Mvvm.Messaging;

namespace History.WindowsClient.Messages;

public sealed class HideLoadingMessage
{
    public static void Send() => WeakReferenceMessenger.Default.Send(new HideLoadingMessage());
}