using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace History.WindowsClient.ViewModels;

// Selects the timeline cell for each post view model kind. The profile cell is
// deferred until the user page work; only repost and post cells are served.
public partial class TimelineTemplateSelector : DataTemplateSelector
{
    public DataTemplate PostTemplate { get; set; }

    public DataTemplate RepostTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item) => item switch
    {
        HistoryRepostViewModel => RepostTemplate,
        HistoryPostViewModel => PostTemplate,
        _ => null,
    };

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container) => SelectTemplateCore(item);
}