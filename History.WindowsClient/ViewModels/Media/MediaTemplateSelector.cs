using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace History.WindowsClient.ViewModels.Media;

public partial class MediaTemplateSelector : DataTemplateSelector
{
    public DataTemplate ImageTemplate { get; set; }

    public DataTemplate VideoTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item) => item switch
    {
        MediaContentViewModel { IsVideo: true } => VideoTemplate,
        MediaContentViewModel => ImageTemplate,
        _ => null,
    };

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container) => SelectTemplateCore(item);
}