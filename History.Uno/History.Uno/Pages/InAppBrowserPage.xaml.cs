using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Web.WebView2.Core;

namespace History.Uno.Pages;

/// <summary>
/// In-app browser page — opens a URL in a WebView2 with a loading indicator.
/// Receives (title, url) via navigation parameter.
/// </summary>
public sealed partial class InAppBrowserPage : Page
{
    public InAppBrowserPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is (string title, string url))
        {
            TitleTextBlock.Text = title;
            BrowserWebView.Source = new Uri(url);
        }
    }

    private void OnBrowserWebViewNavigationStarting(WebView2 sender, CoreWebView2NavigationStartingEventArgs args)
    {
        MainProgressRing.Visibility = Visibility.Visible;
    }

    private void OnBrowserWebViewNavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
    {
        MainProgressRing.Visibility = Visibility.Collapsed;
    }

    private async void OnBackButtonClicked(object sender, RoutedEventArgs e) => await App.PopAsync();
}
