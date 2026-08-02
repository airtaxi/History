using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace History.Uno.ViewModels;

public class TimelineTemplateSelector : DataTemplateSelector
{
    public DataTemplate PostTemplate { get; set; }
    public DataTemplate RepostTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        if (item is RepostViewModel) return RepostTemplate;
        else if (item is PostViewModel) return PostTemplate;
        else throw new ArgumentException("Unknown item type", nameof(item));
    }
}
