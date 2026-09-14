using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using History.Commons.Api.Sticker;
using History.WindowsClient.Views;
using Microsoft.UI.Xaml;
using System.Collections.ObjectModel;

namespace History.WindowsClient.ViewModels.Extras;

// Sticker list page view model: first-page loading, infinite scroll pagination and the
// search query that filters the list. The sticker detail and create windows open modally
// over the owning extras window.
public sealed partial class StickersPageViewModel : BaseViewModel
{
    private readonly SemaphoreSlim _fetchSemaphore = new(1, 1);
    private bool _areThereNoMoreStickersToLoad;
    private string _searchQuery;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    public partial ObservableCollection<StickerViewModel> Items { get; private set; } = [];

    public bool IsEmpty => Items.Count == 0;

    public async Task RefreshAsync()
    {
        if (_fetchSemaphore.CurrentCount == 0) return;

        Items.Clear();
        try
        {
            await _fetchSemaphore.WaitAsync();

            _areThereNoMoreStickersToLoad = false;

            var stickersResult = string.IsNullOrEmpty(_searchQuery) ? await ExecuteRequestAsync(new GetStickers()) : await ExecuteRequestAsync(new SearchStickers(_searchQuery));

            // Swap the whole collection once so the repeater sees a single reset instead of
            // one incremental change per sticker.
            if (stickersResult.IsSuccess) Items = new ObservableCollection<StickerViewModel>(stickersResult.Value.Select(x => new StickerViewModel(x, this)));
            else Items = [];
        }
        finally { _fetchSemaphore.Release(); }
    }

    // Applies a query from the window search box; an empty query restores the full list.
    public async Task SearchAsync(string query)
    {
        _searchQuery = string.IsNullOrWhiteSpace(query) ? null : query.Trim();
        await RefreshAsync();
    }

    [RelayCommand]
    public async Task LoadMoreAsync()
    {
        if (_fetchSemaphore.CurrentCount == 0) return;
        else if (_areThereNoMoreStickersToLoad) return;

        try
        {
            await _fetchSemaphore.WaitAsync();

            var lastViewModel = Items.LastOrDefault();
            if (lastViewModel == null) return;

            var stickersResult = string.IsNullOrEmpty(_searchQuery) ? await ExecuteRequestAsync(new GetStickers(lastViewModel.Id)) : await ExecuteRequestAsync(new SearchStickers(_searchQuery, lastViewModel.Id));
            if (stickersResult.IsSuccess)
            {
                var viewModels = stickersResult.Value.Select(x => new StickerViewModel(x, this)).ToList();

                // One reset per fetched page instead of one incremental change per sticker.
                if (viewModels.Count > 0) Items = new ObservableCollection<StickerViewModel>([.. Items, .. viewModels]);

                if (stickersResult.Value.Count == 0) _areThereNoMoreStickersToLoad = true;
            }
        }
        finally { _fetchSemaphore.Release(); }
    }

    // Opens the sticker detail window modally over this window; the list refreshes when the
    // detail reports a change (subscription state or deletion).
    public void OpenStickerDetail(string stickerId)
    {
        var detailWindow = StickerDetailWindow.ShowModal(stickerId, ExtrasWindow.Instance);
        detailWindow.Closed += OnStickerDetailWindowClosed;
    }

    // Refreshes the list when the closed detail changed sticker data.
    private void OnStickerDetailWindowClosed(object sender, WindowEventArgs args)
    {
        if (sender is not StickerDetailWindow detailWindow) return;
        if (!detailWindow.ViewModel.HasChanges) return;

        _ = RefreshAsync();
    }

    // Opens the sticker create window modally over this window; the list refreshes when the
    // create reports a successful creation.
    public void OpenStickerCreate()
    {
        var createWindow = CreateStickerWindow.ShowModal(ExtrasWindow.Instance);
        createWindow.Closed += OnCreateStickerWindowClosed;
    }

    // Refreshes the list when the closed create window produced a new sticker.
    private void OnCreateStickerWindowClosed(object sender, WindowEventArgs args)
    {
        if (sender is not CreateStickerWindow createWindow) return;
        if (!createWindow.ViewModel.HasCreated) return;

        _ = RefreshAsync();
    }
}
