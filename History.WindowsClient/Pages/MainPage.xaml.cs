using CommunityToolkit.Mvvm.Messaging;
using History.Commons;
using History.WindowsClient.Messages;
using History.WindowsClient.ViewModels;
using History.WindowsClient.ViewModels.MainPage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;

namespace History.WindowsClient.Pages;

public sealed partial class MainPage : BasePage, IRecipient<MainWindowAutoSuggestBoxQuerySubmittedMessage>, IRecipient<RefreshButtonClickedMessage>
{
    protected override MainPageViewModel ViewModel { get; }

    public MainPage()
    {
        ViewModel = App.Services.GetRequiredService<MainPageViewModel>();

        InitializeComponent();

        MainFrame.Navigate(typeof(TimelinePage));

        WeakReferenceMessenger.Default.Register((IRecipient<MainWindowAutoSuggestBoxQuerySubmittedMessage>)this);
        WeakReferenceMessenger.Default.Register((IRecipient<RefreshButtonClickedMessage>)this);
    }

    public void Receive(MainWindowAutoSuggestBoxQuerySubmittedMessage message)
    {
        if (string.IsNullOrWhiteSpace(message.Value)) MainFrame.Navigate(typeof(TimelinePage));
        else MainFrame.Navigate(typeof(SearchResultPage), message.Value);
    }

    private bool _isInForeground;

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        _isInForeground = true;
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);

        _isInForeground = false;
    }

    public void Receive(RefreshButtonClickedMessage message)
    {
        if (_isInForeground)
        {
            _ = ViewModel.RefreshAsync();
            _ = ViewModel.RefreshSideBarAsync();
        }
    }

    private bool _isSyncingModeSelector;
    private bool _isSwitchingMode;

    private void OnModeSelectorBarSelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs e)
    {
        if (_isSyncingModeSelector || _isSwitchingMode) return;

        var isKakaoStoryMode = (sender.SelectedItem?.Tag as string) == "KakaoStory";
        if (isKakaoStoryMode == ViewModel.IsKakaoStoryMode) return;

        _ = ApplyModeAsync(isKakaoStoryMode);
    }

    // Switches the account mode and rebuilds the timeline so the feed matches it.
    private async Task ApplyModeAsync(bool isKakaoStoryMode)
    {
        _isSwitchingMode = true;
        try
        {
            var switched = await ViewModel.SwitchModeAsync(isKakaoStoryMode);
            if (switched) MainFrame.Navigate(typeof(TimelinePage));
        }
        finally
        {
            _isSwitchingMode = false;
            SetModeSelectorSelection(ViewModel.IsKakaoStoryMode);
        }
    }

    // Selects the mode pill without re-entering the selection handler.
    private void SetModeSelectorSelection(bool isKakaoStoryMode)
    {
        _isSyncingModeSelector = true;
        try { ModeSelectorBar.SelectedIndex = isKakaoStoryMode ? 1 : 0; }
        finally { _isSyncingModeSelector = false; }
    }

    private bool _isFirstLoad;
    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_isFirstLoad) return;
        _isFirstLoad = true;

        if (CommonShared.LastUsedKakaoStoryMode)
        {
            // Restore the saved mode; when the login is cancelled the view model stays
            // in History mode and the timeline is rebuilt for it.
            if (!await ViewModel.SwitchModeAsync(true))
            {
                MainFrame.Navigate(typeof(TimelinePage));
            }
        }
        else await ViewModel.RefreshAsync();

        SetModeSelectorSelection(ViewModel.IsKakaoStoryMode);
    }
}
