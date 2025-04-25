using History.MobileClient.ViewModels;
using System.Threading.Tasks;

namespace History.MobileClient.Resources.Styles;

public partial class Profile : ResourceDictionary
{
	public Profile()
	{
		InitializeComponent();
	}

    private void OnEditDescriptionImageTapped(object sender, TappedEventArgs e)
    {
		var image = sender as Image;
        var viewModel = image?.BindingContext as ProfileViewModel;
        viewModel.OnEditDescriptionImageTapped(sender, e);
    }

    private void OnEditNicknameImageTapped(object sender, TappedEventArgs e)
    {
		var image = sender as Image;
        var viewModel = image?.BindingContext as ProfileViewModel;
        viewModel.OnEditNicknameImageTapped(sender, e);
    }

    private async void OnChangeProfileImageBorderTapped(object sender, TappedEventArgs e)
    {
        var border = sender as Border;
        var viewModel = border?.BindingContext as ProfileViewModel;
        await viewModel.HandleChangeProfileMediaAsync();
    }

    private async void OnChangeBackgroundImageButtonClicked(object sender, EventArgs e)
    {
        var button = sender as Button;
        var viewModel = button?.BindingContext as ProfileViewModel;
        await viewModel.HandleChangeBackgroundMediaAsync();
    }
}