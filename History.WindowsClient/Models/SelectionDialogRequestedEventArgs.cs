namespace History.WindowsClient.Models;

public sealed class SelectionDialogRequestedEventArgs(string title, IReadOnlyList<string> options) : EventArgs
{
    public string Title { get; } = title;

    public IReadOnlyList<string> Options { get; } = options;

    public Task<string> ResultTask { get; set; }
}