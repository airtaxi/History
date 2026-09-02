using Microsoft.Windows.Storage.Pickers;

namespace History.WindowsClient.Models;

public sealed class FolderPickerParameters(PickerLocationId? suggestedStartLocation = null, string commitButtonText = null)
{
    public PickerLocationId? SuggestedStartLocation { get; } = suggestedStartLocation;
    public string CommitButtonText { get; } = commitButtonText;
}