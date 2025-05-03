using History.Commons.Api.User;
using History.MobileClient.Pages;
using History.MobileClient.ViewModels;
using System.Threading.Tasks;

namespace History.MobileClient.Resources.Styles;

public partial class Post : ResourceDictionary
{
	public Post()
	{
		InitializeComponent();
	}

    private async void OnTapped(object sender, TappedEventArgs e)
    {
		var viewModel = (sender as Element)?.BindingContext as PostViewModel;
		var newViewModel = new PostViewModel(viewModel.Post, false);
        var postPage = new PostPage(newViewModel);
		await App.PushModalAsync(postPage);
    }

    private async void OnProfileImageTapped(object sender, TappedEventArgs e)
    {
        var element = sender as Element;
        var viewModel = element.BindingContext as PostViewModel;
        var profilePage = new UserPage(viewModel.Post.User.UserId);
        await App.PushModalAsync(profilePage);
    }
}