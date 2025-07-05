using History.MobileClient.ContentViews;
using History.MobileClient.Pages;
using History.MobileClient.ViewModels;

namespace History.MobileClient.Resources.Styles;

public partial class Post : ResourceDictionary
{
	public Post()
	{
		InitializeComponent();
    }

    private async void OnHashtagBorderTapped(object sender, TappedEventArgs e)
    {
        var border = sender as Border;
        var hashtag = border?.BindingContext as string;
        if (string.IsNullOrEmpty(hashtag)) return;

        var page = new EditPostPage([hashtag]);
        await App.PushAsync(page);
    }
}