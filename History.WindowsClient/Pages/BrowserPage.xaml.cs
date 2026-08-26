using History.WindowsClient.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace History.WindowsClient.Pages;

public sealed partial class BrowserPage : Page
{
    private string _url;

    public BrowserPage() => InitializeComponent();

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is not BrowserPageParameters parameters) return;
        _url = parameters.Url;
    }

    private async void OnMainWebViewLoaded(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_url)) return;

        await MainWebView.EnsureCoreWebView2Async();
        MainWebView.CoreWebView2.Navigate(_url);
    }
}