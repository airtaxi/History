using CommunityToolkit.Mvvm.ComponentModel;
using History.WindowsClient.Models;
using Microsoft.UI.Xaml.Controls;

namespace History.WindowsClient.ViewModels;

public abstract partial class BaseViewModel : ObservableObject
{
    public event EventHandler<MessageDialogRequestedEventArgs> MessageDialogRequested;

    public async Task<ContentDialogResult?> ShowMessageDialogAsync(MessageDialogParameters parameters)
    {
        var args = new MessageDialogRequestedEventArgs(parameters);
        MessageDialogRequested?.Invoke(this, args);
        if (args.ResultTask == null) return null;

        return await args.ResultTask;
    }
}
