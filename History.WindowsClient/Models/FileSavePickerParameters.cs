using Microsoft.Windows.Storage.Pickers;

namespace History.WindowsClient.Models;

public sealed class FileSavePickerParameters(IReadOnlyDictionary<string, IReadOnlyList<string>> fileTypeChoices, string suggestedFileName = null, string defaultFileExtension = null, PickerLocationId? suggestedStartLocation = null)
{
    public IReadOnlyDictionary<string, IReadOnlyList<string>> FileTypeChoices { get; } = fileTypeChoices;
    public string SuggestedFileName { get; } = suggestedFileName;
    public string DefaultFileExtension { get; } = defaultFileExtension;
    public PickerLocationId? SuggestedStartLocation { get; } = suggestedStartLocation;
}