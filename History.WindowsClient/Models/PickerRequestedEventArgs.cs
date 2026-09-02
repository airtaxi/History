namespace History.WindowsClient.Models;

// Picker counterpart of MessageDialogRequestedEventArgs: carries the picker parameters
// to the host page and receives the picked result. Generic over the parameters and the
// result type so the open/save/folder pickers share one request shape.
public sealed class PickerRequestedEventArgs<TParameters, TResult>(TParameters parameters) : EventArgs
{
    public TParameters Parameters { get; } = parameters;

    public Task<TResult> ResultTask { get; set; }
}