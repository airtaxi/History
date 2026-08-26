namespace History.WindowsClient.Models;

public sealed class MessageDialogParameters(string title, string description, string primaryButtonText = null, string secondaryButtonText = null)
{
    public string Title { get; } = title;
    public string Description { get; } = description;
    public string PrimaryButtonText { get; } = primaryButtonText;
    public string SecondaryButtonText { get; } = secondaryButtonText;
}
