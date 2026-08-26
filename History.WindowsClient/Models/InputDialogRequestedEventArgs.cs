namespace History.WindowsClient.Models;

public sealed class InputDialogRequestedEventArgs(InputDialogParameters parameters) : EventArgs
{
    public InputDialogParameters Parameters { get; } = parameters;

    public Task<string> ResultTask { get; set; }
}
