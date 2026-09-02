using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;

namespace History.WindowsClient.ViewModels.MainPage;

public partial class MainPageMessagesSideBarViewModel(MainPageViewModel baseViewModel) : BaseMainPageSideBarViewModel
{
    public MainPageViewModel BaseViewModel { get; } = baseViewModel;
}
