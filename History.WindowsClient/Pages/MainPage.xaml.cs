using CommunityToolkit.Mvvm.Messaging;
using History.Commons;
using History.WindowsClient.Enums;
using History.WindowsClient.Messages;
using History.WindowsClient.Services;
using History.WindowsClient.ViewModels;
using History.WindowsClient.ViewModels.MainPage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;

namespace History.WindowsClient.Pages;

public sealed partial class MainPage : BasePage, IRecipient<MainWindowAutoSuggestBoxQuerySubmittedMessage>, IRecipient<RefreshRequestedMessage>, IRecipient<MainFeedModeRequestedMessage>
{
    protected override MainPageViewModel ViewModel { get; }

    public MainPage()
    {
        ViewModel = App.Services.GetRequiredService<MainPageViewModel>();

        InitializeComponent();

        // The poller keeps the side bar badge counts fresh while the user is elsewhere in the app.
        App.Services.GetRequiredService<BadgePollerService>().AddRefreshTarget(ViewModel.RefreshBadgeCountsAsync);

        SetFeedMode(MainFeedMode.Timeline);

        WeakReferenceMessenger.Default.Register((IRecipient<MainWindowAutoSuggestBoxQuerySubmittedMessage>)this);
        WeakReferenceMessenger.Default.Register((IRecipient<RefreshRequestedMessage>)this);
        WeakReferenceMessenger.Default.Register((IRecipient<MainFeedModeRequestedMessage>)this);
    }

    public void Receive(MainWindowAutoSuggestBoxQuerySubmittedMessage message)
    {
        // Searching leaves the active feed, so the toggles are cleared before the frame changes.
        ResetFeedMode();

        if (string.IsNullOrWhiteSpace(message.Value)) MainFrame.Navigate(typeof(TimelinePage));
        else MainFrame.Navigate(typeof(SearchResultPage), message.Value);
    }

    public void Receive(MainFeedModeRequestedMessage message) => SetFeedMode(message.Value);

    // Switches the left feed area between the timeline, discover and bookmarks feeds, then
    // notifies the window so every title bar toggle stays in sync.
    private void SetFeedMode(MainFeedMode mode)
    {
        _feedMode = mode;
        MainFrame.Navigate(mode switch
        {
            MainFeedMode.Discover => typeof(PublicPostsPage),
            MainFeedMode.Bookmarks => typeof(BookmarkedPostsPage),
            _ => typeof(TimelinePage),
        });
        WeakReferenceMessenger.Default.Send(new MainFeedModeChangedMessage(_feedMode));
    }

    // Clears the feed mode without touching the frame, for paths that navigate elsewhere.
    private void ResetFeedMode()
    {
        if (_feedMode == MainFeedMode.Timeline) return;

        _feedMode = MainFeedMode.Timeline;
        WeakReferenceMessenger.Default.Send(new MainFeedModeChangedMessage(MainFeedMode.Timeline));
    }

    private MainFeedMode _feedMode = MainFeedMode.Timeline;

    public void Receive(RefreshRequestedMessage message)
    {
        if (message.XamlRoot != XamlRoot) return;
        if (IsInForeground)
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
            if (switched) SetFeedMode(MainFeedMode.Timeline);
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

    protected override async void OnFirstPageLoad()
    {
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
