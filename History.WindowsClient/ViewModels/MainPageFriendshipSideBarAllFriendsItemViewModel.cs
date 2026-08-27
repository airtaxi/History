using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using History.Commons;
using History.Commons.Api.Friendship;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using History.Commons.Helpers;
using History.WindowsClient.Messages;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;

namespace History.WindowsClient.ViewModels;

public partial class MainPageFriendshipSideBarAllFriendsItemViewModel : BaseMainPageFriendshipSideBarItemViewModel, IRecipient<FriendshipChangedMessage>
{
    private ObservableCollection<BaseFriendshipViewModel> _items;

    public MainPageFriendshipSideBarAllFriendsItemViewModel(MainPageFriendshipSideBarViewModel parentViewModel) : base(parentViewModel)
    {
        if (!Parent.Parent.IsKakaoStoryMode) SearchAutoSuggestBoxPlaceholderText = "친구의 닉네임 또는 핸들 검색";
        RightHeaderText = "친구 목록";

        Query = string.Empty;
        EmptyText = "친구 목록이 비어있습니다";

        WeakReferenceMessenger.Default.Register(this);
    }

    public void Receive(FriendshipChangedMessage message)
    {
        if (Parent.Parent.IsKakaoStoryMode) return; // Kakao Story friends are not tracked by the History friendship message.

        var data = message.Value;
        var isFriend = data.NewStatus == FriendshipStatus.Accepted;
        var existingViewModel = _items?.OfType<HistoryFriendshipViewModel>().FirstOrDefault(x => x.User.UserId == data.UserId);

        // Keep CommonShared.Friends in sync regardless of the target list, since it is used across the app.
        if (isFriend)
        {
            if (CommonShared.Friends != null && !CommonShared.Friends.Any(x => x.UserId == data.UserId))
            {
                CommonShared.Friends.Add(data.User);
            }
        }
        else CommonShared.Friends?.RemoveAll(x => x.UserId == data.UserId);

        if (_items == null) return; // First load has not happened yet; it will fetch the latest data.

        if (isFriend && existingViewModel == null) _items.Add(new HistoryFriendshipViewModel(data.User, Parent.Parent) { FriendshipVisibility = Visibility.Collapsed });
        else if (!isFriend && existingViewModel != null) _items.Remove(existingViewModel);

        _items = new(_items.OrderByDescending(x => x.IsFavorite).ThenBy(x => x.Nickname));
        RightHeaderText = $"친구 목록 (총 {_items.Count}명)";
        IsEmpty = _items.Count == 0;
        ApplyQuery(Query);
    }

    public override async Task RefreshAsync()
    {
        if (!Parent.Parent.IsKakaoStoryMode)
        {
            var result = await App.ExecuteRequestAsync(new GetFriends(CommonShared.UserId));
            if (!result.IsSuccess)
            {
                await Parent.Parent.ShowMessageDialogAsync(new("오류", "친구 목록을 가져오는 데에 실패하였습니다."));
                return;
            }

            _items = new(result.Value.OrderByDescending(x => x.IsFavorite).ThenBy(x => x.Nickname).Select(x => new HistoryFriendshipViewModel(x, Parent.Parent) { FriendshipVisibility = Visibility.Collapsed }));
            Items = _items;
            RightHeaderText = $"친구 목록 (총 {result.Value.Count}명)";

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
