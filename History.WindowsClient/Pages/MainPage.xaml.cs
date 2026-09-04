using CommunityToolkit.Mvvm.Messaging;
using History.WindowsClient.Messages;
using History.WindowsClient.ViewModels;
using History.WindowsClient.ViewModels.MainPage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;

namespace History.WindowsClient.Pages;

public sealed partial class MainPage : BasePage, IRecipient<MainWindowAutoSuggestBoxQuerySubmittedMessage>
{
    protected override MainPageViewModel ViewModel { get; }

    public MainPage()
    {
        ViewModel = App.Services.GetRequiredService<MainPageViewModel>();

        InitializeComponent();

        MainFrame.Navigate(typeof(TimelinePage));

        WeakReferenceMessenger.Default.Register(this);
    }

    public void Receive(MainWindowAutoSuggestBoxQuerySubmittedMessage message)
    {
        if (string.IsNullOrWhiteSpace(message.Value)) MainFrame.Navigate(typeof(TimelinePage));
        else MainFrame.Navigate(typeof(SearchResultPage), message.Value);
    }

    private bool _isFirstLoad;
    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_isFirstLoad) return;
        _isFirstLoad = true;

        await ViewModel.RefreshAsync();
    }
}
