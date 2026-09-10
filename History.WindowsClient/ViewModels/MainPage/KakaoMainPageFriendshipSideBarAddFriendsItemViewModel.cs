using History.Commons;
using History.Commons.KakaoStory;
using History.WindowsClient.Helpers;
using Microsoft.UI.Xaml.Controls;
using static History.Commons.KakaoStory.KakaoStoryApiHandler.DataType;

namespace History.WindowsClient.ViewModels.MainPage;

public partial class KakaoMainPageFriendshipSideBarAddFriendsItemViewModel : BaseMainPageFriendshipSideBarItemViewModel
{
    private long _searchSequence;

    public KakaoMainPageFriendshipSideBarAddFriendsItemViewModel(MainPageViewModel baseViewModel) : base(baseViewModel)
    {
        SearchAutoSuggestBoxPlaceholderText = "카카오스토리 ID 검색";
        RightHeaderText = "검색 결과";
        Query = string.Empty;
        EmptyText = "검색 결과가 없습니다";
        IsEmpty = true;
    }

    // Refresh is a no-op for the add friends item.
    public override Task RefreshAsync() => Task.CompletedTask;

    // TextChanged is a no-op; the search runs when the query is submitted.
    public override void OnFriendshipSideBarAutoSuggestBoxTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args) { }
    public override void OnFriendshipSideBarAutoSuggestBoxQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args) => ApplyQuery(sender.Text);

    private async void ApplyQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return;

        var sequence = ++_searchSequence;

        if (!await KakaoStoryUtils.EnsureLoggedInAsync(BaseViewModel)) return;

        SearchData.SearchResults searchResults;
        try { searchResults = await BaseViewModel.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.SearchUsers(query)); }
        catch (Exception exception)
        {
            if (sequence == _searchSequence) await BaseViewModel.ShowMessageDialogAsync(new(Constants.ErrorTitle, $"친구 검색에 실패하였습니다.\n{exception.Message}"));
            return;
        }

        if (sequence != _searchSequence) return; // A newer search was issued; discard stale results.

        Items = new((searchResults?.search_results ?? []).Select(x => (BaseFriendshipViewModel)new KakaoFriendshipViewModel(x, BaseViewModel)));
        IsEmpty = Items.Count == 0;
    }
}
