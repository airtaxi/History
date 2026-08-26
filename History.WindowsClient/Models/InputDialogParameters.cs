namespace History.WindowsClient.Models;

public sealed class InputDialogParameters(string title, string placeholderText = "", bool showCancel = false, bool numberOnly = false, string defaultText = "")
{
    public string Title { get; } = title;
    public string PlaceholderText { get; } = placeholderText;
    public bool ShowCancel { get; } = showCancel;
    public bool NumberOnly { get; } = numberOnly;
    public string DefaultText { get; } = defaultText;
}
