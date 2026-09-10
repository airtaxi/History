using History.Commons;
using History.Commons.Helpers;
using History.Commons.KakaoStory;
using History.WindowsClient.Helpers;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using static History.Commons.KakaoStory.KakaoStoryApiHandler.DataType;

namespace History.WindowsClient.ViewModels.MainPage;

public partial class KakaoMainPageFriendshipSideBarAllFriendsItemViewModel : BaseMainPageFriendshipSideBarItemViewModel
{
    private ObservableCollection<BaseFriendshipViewModel> _items = [];

    public KakaoMainPageFriendshipSideBarAllFriendsItemViewModel(MainPageViewModel baseViewModel) : base(baseViewModel)
    {
        SearchAutoSuggestBoxPlaceholderText = "친구의 닉네임 검색";
        RightHeaderText = "친구 목록";
        Query = string.Empty;
        EmptyText = "친구 목록이 비어있습니다";
        IsEmpty = true;
    }

    public override async Task RefreshAsync()
    {
        if (!await KakaoStoryUtils.EnsureLoggedInAsync(BaseViewModel)) return;

        FriendData.Friends friends;
        try { friends = await BaseViewModel.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.GetFriends()); }
        catch (Exception exception)
        {
            await BaseViewModel.ShowMessageDialogAsync(new(Constants.ErrorTitle, $"친구 목록을 가져오는 데에 실패하였습니다.\n{exception.Message}"));
            return;
        }

        _items = new((friends?.profiles ?? []).OrderBy(x => x.display_name).Select(x => (BaseFriendshipViewModel)new KakaoFriendshipViewModel(x, BaseViewModel)));
        Items = _items;
        RightHeaderText = $"친구 목록 (총 {_items.Count}명)";
        IsEmpty = _items.Count == 0;
        ApplyQuery(Query);
    }

    public override void OnFriendshipSideBarAutoSuggestBoxTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args) => ApplyQuery(sender.Text);
    public override void OnFriendshipSideBarAutoSuggestBoxQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args) => ApplyQuery(sender.Text);

    private void ApplyQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) Items = _items;
        else Items = new(_items.Where(x => x.Nickname.Contains(query, StringComparison.OrdinalIgnoreCase) || KoreanHelper.SplitToChosung(x.Nickname).Contains(query, StringComparison.OrdinalIgnoreCase)));
    }
}
