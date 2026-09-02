using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using History.Commons;
using History.Commons.Api.Friendship;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Helpers;
using History.WindowsClient.Messages;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;

namespace History.WindowsClient.ViewModels.MainPage;

public partial class MainPageFriendshipSideBarPendingFriendsItemViewModel : BaseMainPageFriendshipSideBarItemViewModel, IRecipient<FriendshipChangedMessage>
{
    private ObservableCollection<BaseFriendshipViewModel> _items;

    public MainPageFriendshipSideBarPendingFriendsItemViewModel(MainPageViewModel baseViewModel) : base(baseViewModel)
    {
        if (!BaseViewModel.IsKakaoStoryMode) SearchAutoSuggestBoxPlaceholderText = "친구의 닉네임 또는 핸들 검색";
        else SearchAutoSuggestBoxPlaceholderText = "친구의 닉네임 검색";

        Query = string.Empty;
        RightHeaderText = "받은 친구 신청 목록";
        EmptyText = "비어있음";

        WeakReferenceMessenger.Default.Register(this);
    }

    public void Receive(FriendshipChangedMessage message)
    {
        if (BaseViewModel.IsKakaoStoryMode) return; // Kakao Story friends are not tracked by the History friendship message.

        var data = message.Value;
        if (_items == null) return; // First load has not happened yet; it will fetch the latest data.

        var removedViewModel = _items.OfType<HistoryFriendshipViewModel>().FirstOrDefault(x => x.User.UserId == data.UserId);
        if (removedViewModel == null) return;

        _items.Remove(removedViewModel);
        IsEmpty = _items.Count == 0;
        ApplyQuery(Query);
    }

    public override async Task RefreshAsync()
    {
        if (!BaseViewModel.IsKakaoStoryMode)
        {
            var result = await App.ExecuteRequestAsync(new GetPendingRequests());
            if (!result.IsSuccess)
            {
                await BaseViewModel.ShowMessageDialogAsync(new("오류", "받은 친구 신청 목록을 가져오는 데에 실패하였습니다."));
                return;
            }

            _items = new(result.Value.OrderByDescending(x => x.IsFavorite).ThenBy(x => x.Nickname).Select(x => new HistoryFriendshipViewModel(x, BaseViewModel) { FriendshipVisibility = Visibility.Visible}));
            Items = _items;

            RightHeaderText = $"받은 친구 신청 목록 (총 {result.Value.Count}명)";
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
