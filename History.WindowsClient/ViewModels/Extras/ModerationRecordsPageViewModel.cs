using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using History.Commons.Api.Moderation;
using System.Collections.ObjectModel;

namespace History.WindowsClient.ViewModels.Extras;

// Moderation records page view model: first-page loading and infinite scroll pagination of the
// restriction records that moderators can review.
public partial class ModerationRecordsPageViewModel : BaseViewModel
{
    private readonly SemaphoreSlim _fetchSemaphore = new(1, 1);
    private bool _areThereNoMoreRecordsToLoad;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    public partial ObservableCollection<ModerationRecordViewModel> Items { get; private set; } = [];

    public bool IsEmpty => Items.Count == 0;

    public async Task RefreshAsync()
    {
        if (_fetchSemaphore.CurrentCount == 0) return;

        Items.Clear();
        try
        {
            await _fetchSemaphore.WaitAsync();

            _areThereNoMoreRecordsToLoad = false;

            var recordsResult = await ExecuteRequestAsync(new GetModerationRecords(null, Constants.PageSize));

            // Swap the whole collection once so the repeater sees a single reset instead of
            // one incremental change per record.
            if (recordsResult.IsSuccess) Items = new ObservableCollection<ModerationRecordViewModel>(recordsResult.Value.Select(x => new ModerationRecordViewModel(x, this)));
            else Items = [];
        }
        finally { _fetchSemaphore.Release(); }
    }

    [RelayCommand]
    public async Task LoadMoreAsync()
    {
        if (_fetchSemaphore.CurrentCount == 0) return;
        else if (_areThereNoMoreRecordsToLoad) return;

        try
        {
            await _fetchSemaphore.WaitAsync();

            var lastViewModel = Items.LastOrDefault();
            if (lastViewModel == null) return;

            var recordsResult = await ExecuteRequestAsync(new GetModerationRecords(lastViewModel.Id, Constants.PageSize));
            if (recordsResult.IsSuccess)
            {
                var viewModels = recordsResult.Value.Select(x => new ModerationRecordViewModel(x, this)).ToList();

                // One reset per fetched page instead of one incremental change per record.
                if (viewModels.Count > 0) Items = new ObservableCollection<ModerationRecordViewModel>([.. Items, .. viewModels]);

                if (recordsResult.Value.Count == 0) _areThereNoMoreRecordsToLoad = true;
            }
        }
        finally { _fetchSemaphore.Release(); }
    }
}
