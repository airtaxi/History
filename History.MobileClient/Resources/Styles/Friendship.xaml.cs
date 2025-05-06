using History.MobileClient.ViewModels;

namespace History.MobileClient.Resources.Styles;

public partial class Friendship : ResourceDictionary
{
	public Friendship()
	{
		InitializeComponent();
	}

    private async void OnFriendshipImageTapped(object sender, TappedEventArgs e)
    {
		var image = sender as Image;
		var viewModel = image?.BindingContext as FriendshipViewModel;
        await viewModel?.HandleFriendshipActionAsync();
    }

    private async void OnTapped(object sender, TappedEventArgs e)
    {
        var element = sender as Element;
        var viewModel = element?.BindingContext as FriendshipViewModel;
        await viewModel?.HandleTapAsync();
    }
}