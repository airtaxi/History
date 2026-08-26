using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace History.WindowsClient.ViewModels;

public class MainPageSideBarDataTemplateSelector : DataTemplateSelector
{
    public DataTemplate Friendship { get; set; }
    public DataTemplate Messages { get; set; }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        if (item is not BaseMainPageSideBarViewModel sideBarViewModel) return base.SelectTemplateCore(item, container);
        if (sideBarViewModel is MainPageFriendshipSideBarViewModel) return Friendship;
        else return Messages;
    }
}
