using History.Commons.DataTypes.Contents;

namespace History.WindowsClient.ViewModels.Content;

// Wraps an external URL preview content for the ExternalUrlContentControl.
public sealed partial class ExternalUrlContentItemViewModel(ExternalUrlContent externalUrlContent, BaseViewModel baseViewModel) : IContentViewModel
{
    public ExternalUrlContent ExternalUrlContent { get; } = externalUrlContent;
    public BaseViewModel BaseViewModel { get; } = baseViewModel;
}