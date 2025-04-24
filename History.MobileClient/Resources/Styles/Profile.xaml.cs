using History.MobileClient.ViewModels;

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
}