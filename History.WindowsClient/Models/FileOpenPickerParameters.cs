using Microsoft.Windows.Storage.Pickers;

namespace History.WindowsClient.Models;

public sealed class FileOpenPickerParameters(IReadOnlyList<string> fileTypeFilters, PickerLocationId? suggestedStartLocation = null, string commitButtonText = null)
{
    public IReadOnlyList<string> FileTypeFilters { get; } = fileTypeFilters;
    public PickerLocationId? SuggestedStartLocation { get; } = suggestedStartLocation;
    public string CommitButtonText { get; } = commitButtonText;
}