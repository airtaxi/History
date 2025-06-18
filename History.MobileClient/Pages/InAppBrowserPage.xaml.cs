namespace History.MobileClient.Pages;

public partial class InAppBrowserPage : ContentPage
{
    private readonly string _url;


    public InAppBrowserPage(string title, string url)
	{
		InitializeComponent();
        _url = url;
        Title = title;
        TitleLabel.Text = title;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        BrowserWebView.Source = _url;
    }

    private void OnNavigating(object sender, WebNavigatingEventArgs e)
    {
        MainActivityIndicator.IsVisible = true;
        MainActivityIndicator.IsRunning = true;
    }

    private void OnNavigated(object sender, WebNavigatedEventArgs e)
    {
        MainActivityIndicator.IsVisible = false;
        MainActivityIndicator.IsRunning = false;
    }

    private async void OnBackImageTapped(object sender, TappedEventArgs e) => await App.PopModalAsync();
}