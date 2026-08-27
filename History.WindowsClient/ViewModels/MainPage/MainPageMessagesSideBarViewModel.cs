using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;

namespace History.WindowsClient.ViewModels.MainPage;

public partial class MainPageMessagesSideBarViewModel(MainPageViewModel parentViewModel) : BaseMainPageSideBarViewModel
{
    public MainPageViewModel Parent { get; } = parentViewModel;
}
