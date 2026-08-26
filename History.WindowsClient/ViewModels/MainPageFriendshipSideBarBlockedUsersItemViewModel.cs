using CommunityToolkit.Mvvm.ComponentModel;
using History.Commons;
using History.Commons.Api.Friendship;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;

namespace History.WindowsClient.ViewModels;

public partial class MainPageFriendshipSideBarBlockedUsersItemViewModel : BaseMainPageFriendshipSideBarItemViewModel
{
    private ObservableCollection<BaseFriendshipViewModel> _items;

    public MainPageFriendshipSideBarBlockedUsersItemViewModel(MainPageFriendshipSideBarViewModel parentViewModel) : base(parentViewModel)
    {
        if (!Parent.Parent.IsKakaoStoryMode) SearchAutoSuggestBoxPlaceholderText = "친구의 닉네임 또는 핸들 검색";
        else SearchAutoSuggestBoxPlaceholderText = "친구의 닉네임 검색";

        Query = string.Empty;
        RightHeaderText = "차단한 사용자 목록";
        EmptyText = "차단한 사용자가 없습니다";
    }             

    public override async Task RefreshAsync()
    {
        if (!Parent.Parent.IsKakaoStoryMode)
        {
            var result = await App.ExecuteRequestAsync(new GetBlockedUsers());
            if (!result.IsSuccess)
            {
                await Parent.Parent.ShowMessageDialogAsync(new("오류", "차단한 사용자 목록을 가져오는 데에 실패하였습니다."));
                return;
            }

            _items = new(result.Value.OrderByDescending(x => x.IsFavorite).ThenBy(x => x.Nickname).Select(x => new HistoryFriendshipViewModel(x) { FriendshipVisibility = Visibility.Visible}));
            Items = _items;

            RightHeaderText = $"차단한 사용자 목록 (총 {result.Value.Count}명)";
            IsEmpty = _items.Count == 0;
        }
    }

    public override void OnFriendshipSideBarAutoSuggestBoxTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args) => ApplyQuery(sender.Text);
    public override void OnFriendshipSideBarAutoSuggestBoxQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args) => ApplyQuery(sender.Text);

    private void ApplyQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) Items = _items;
        else Items = new(_items.Where(x => x.Nickname.Contains(query, StringComparison.OrdinalIgnoreCase) || KoreanHelper.SplitToChosung(x.Nickname).Contains(query, StringComparison.OrdinalIgnoreCase)
                || (x is HistoryFriendshipViewModel historyFriendshipViewModel && historyFriendshipViewModel.User.Handle.Contains(query, StringComparison.OrdinalIgnoreCase))));
    }
}
