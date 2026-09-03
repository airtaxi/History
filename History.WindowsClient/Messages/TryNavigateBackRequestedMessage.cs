using CommunityToolkit.Mvvm.Messaging;
using History.WindowsClient.Models;
using Microsoft.UI.Xaml;

namespace History.WindowsClient.Messages;

public sealed class TryNavigateBackRequestedMessage(XamlRoot xamlRoot)
{
    private readonly TaskCompletionSource<bool> _taskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public XamlRoot XamlRoot { get; } = xamlRoot;

    public Task<bool> Completion => _taskCompletionSource.Task;

    public void Complete(bool result) => _taskCompletionSource.TrySetResult(result);

    public static void Send(XamlRoot xamlRoot, TryNavigateBackRequestedEventArgs args)
    {
        var message = new TryNavigateBackRequestedMessage(xamlRoot);
        args.ResultTask = message.Completion;
        WeakReferenceMessenger.Default.Send(message);
    }
}
