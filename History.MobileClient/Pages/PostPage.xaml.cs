namespace History.MobileClient.Pages;

public partial class PostPage : ContentPage
{
	private string _postId;
    public PostPage(string postId)
	{
		_postId = postId;
        InitializeComponent();
	}

    private async void OnBackImageTapped(object sender, TappedEventArgs e) => await App.PopModalAsync();
}