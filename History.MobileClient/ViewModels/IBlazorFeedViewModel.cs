using System.Collections.ObjectModel;

namespace History.MobileClient.ViewModels;

// Common feed surface consumed by the Blazor MasonryFeed component. Every feed page's
// view model (timeline, public posts, search, bookmarks) implements it so the shared
// component can render items, drive infinite scroll and pull-to-refresh, and request
// scroll-to-top without knowing the concrete data source.
public interface IBlazorFeedViewModel
{
    ObservableCollection<BasePostViewModel> Items { get; }
    event Action ScrollToTopRequested;
    Task RefreshAsync();
    Task LoadMoreAsync();
    void SetScrollToTopVisible(bool isVisible);
}
