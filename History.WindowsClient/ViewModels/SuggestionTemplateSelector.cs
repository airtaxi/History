using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace History.WindowsClient.ViewModels;

public partial class SuggestionTemplateSelector : DataTemplateSelector
{
    public DataTemplate FriendshipTemplate { get; set; }

    public DataTemplate HashtagTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item) =>
        item switch
        {
            BaseFriendshipViewModel => FriendshipTemplate,
            string => HashtagTemplate,
            _ => null,
        };

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container) => SelectTemplateCore(item);
}