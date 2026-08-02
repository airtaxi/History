using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace History.Uno.ViewModels;

public class MediaTemplateSelector : DataTemplateSelector
{
    public DataTemplate VideoTemplate { get; set; }
    public DataTemplate ImageTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        if (item is ImageViewModel) return ImageTemplate;
        else if (item is VideoViewModel) return VideoTemplate;
        else throw new ArgumentException("Unknown item type", nameof(item));
    }
}
