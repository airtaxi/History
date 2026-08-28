using History.Commons.DataTypes.Contents;

namespace History.WindowsClient.ViewModels;

// Wraps an external URL preview content for the ExternalUrlContentControl.
public sealed partial class ExternalUrlContentItemViewModel(ExternalUrlContent externalUrlContent) : IContentViewModel
{
    public ExternalUrlContent ExternalUrlContent { get; } = externalUrlContent;
}