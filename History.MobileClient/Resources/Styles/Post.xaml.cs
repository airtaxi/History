using History.MobileClient.Pages;
using History.MobileClient.ViewModels;

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
}