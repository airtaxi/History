using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;

namespace History.WindowsClient.ViewModels;

public partial class MainPageMessagesSideBarViewModel(MainPageViewModel parentViewModel) : BaseMainPageSideBarViewModel
{
    public MainPageViewModel Parent { get; } = parentViewModel;
}
