namespace History.MobileClient.Pages;

public partial class TimelinePage : ContentPage
{
	public TimelinePage()
	{
		InitializeComponent();
	}

    private async void OnCreateNewPostImageButtonClicked(object sender, EventArgs e)
    {
		await App.MainWindow.Page.Navigation.PushModalAsync(new EditPostPage());
    }
}