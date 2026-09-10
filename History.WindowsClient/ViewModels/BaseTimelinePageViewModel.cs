using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace History.WindowsClient.ViewModels;

// Shared surface for the timeline feed: post items, empty state and the
// refresh/load-more contract implemented by each account mode feed.
public abstract partial class BaseTimelinePageViewModel : BaseViewModel
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    public partial ObservableCollection<BasePostViewModel> Items { get; protected set; } = [];

    public bool IsEmpty => Items.Count == 0;

    public abstract Task RefreshAsync();

    [RelayCommand]
    public virtual Task LoadMoreAsync() => throw new NotSupportedException("[BaseTimelinePageViewModel] LoadMoreAsync must be overridden");
}
