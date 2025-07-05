using History.MobileClient.Pages;

namespace History.MobileClient.Resources.Styles;

public partial class SharedPost : ResourceDictionary
{
    public SharedPost()
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
