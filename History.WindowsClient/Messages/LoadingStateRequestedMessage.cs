using CommunityToolkit.Mvvm.Messaging;
using History.WindowsClient.Models;
using Microsoft.UI.Xaml;

namespace History.WindowsClient.Messages;

public sealed class LoadingStateRequestedMessage(XamlRoot xamlRoot, string loadingMessage, Func<Task> action)
{
    private readonly TaskCompletionSource _taskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public XamlRoot XamlRoot { get; } = xamlRoot;

    public string LoadingMessage { get; } = loadingMessage;

    public Func<Task> Action { get; } = action;

    public Task Completion => _taskCompletionSource.Task;

    public void Complete() => _taskCompletionSource.TrySetResult();

    public void Fail(Exception exception) => _taskCompletionSource.TrySetException(exception);

    public static void Send(XamlRoot xamlRoot, LoadingStateRequestedEventArgs args)
    {
        var message = new LoadingStateRequestedMessage(xamlRoot, args.LoadingMessage, args.Action);
        args.ResultTask = message.Completion;
        WeakReferenceMessenger.Default.Send(message);
    }
}