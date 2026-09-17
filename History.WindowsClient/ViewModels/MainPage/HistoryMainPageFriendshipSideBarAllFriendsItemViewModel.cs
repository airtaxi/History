using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using History.Commons;
using History.Commons.Api.Friendship;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using History.Commons.Helpers;
using History.WindowsClient.Messages;
using History.WindowsClient.Models;
using History.WindowsClient.ViewModels.Friendship;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;

namespace History.WindowsClient.ViewModels.MainPage;

public partial class HistoryMainPageFriendshipSideBarAllFriendsItemViewModel : BaseMainPageFriendshipSideBarItemViewModel, IRecipient<FriendshipChangedMessage>
{
    private readonly ApplicationSettings _settings = App.Services.GetRequiredService<ApplicationSettings>();

    private ObservableCollection<BaseFriendshipViewModel> _items;
    private bool _sortByTime;

    public HistoryMainPageFriendshipSideBarAllFriendsItemViewModel(MainPageViewModel baseViewModel) : base(baseViewModel)
    {
        SearchAutoSuggestBoxPlaceholderText = "친구의 닉네임 또는 핸들 검색";
        RightHeaderText = "친구 목록";

        Query = string.Empty;
        EmptyText = "친구 목록이 비어있습니다";

        _sortByTime = _settings.IsFriendsListSortedByTime;
        IsSortButtonVisible = true;
        ApplySort();

        WeakReferenceMessenger.Default.Register(this);
    }

    // The header toggle flips the list between name order and friendship-date order, and the
    // last used order is restored from the application settings on the next visit.
    public override void HandleSortTap()
    {
        _sortByTime = !_sortByTime;
        ApplySort();
    }

    public void Receive(FriendshipChangedMessage message)
    {
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

        if (isFriend && existingViewModel == null) _items.Add(new HistoryFriendshipViewModel(data.User, BaseViewModel) { FriendshipVisibility = Visibility.Collapsed });
        else if (!isFriend && existingViewModel != null) _items.Remove(existingViewModel);

        RightHeaderText = $"친구 목록 (총 {_items.Count}명)";
        IsEmpty = _items.Count == 0;
        ApplyFilterAndSort(Query);
    }

    public override async Task RefreshAsync()
    {
        var result = await BaseViewModel.ExecuteRequestAsync(new GetFriends(CommonShared.UserId));
        if (!result.IsSuccess)
        {
            await BaseViewModel.ShowMessageDialogAsync(new(Constants.ErrorTitle, "친구 목록을 가져오는 데에 실패하였습니다."));
            return;
        }

        _items = new(result.Value.Select(x => new HistoryFriendshipViewModel(x, BaseViewModel) { FriendshipVisibility = Visibility.Collapsed }));
        RightHeaderText = $"친구 목록 (총 {result.Value.Count}명)";

        IsEmpty = _items.Count == 0;
        ApplyFilterAndSort(Query);
    }

    public override void OnFriendshipSideBarAutoSuggestBoxTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args) => ApplyFilterAndSort(sender.Text);
    public override void OnFriendshipSideBarAutoSuggestBoxQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args) => ApplyFilterAndSort(sender.Text);

    private void ApplySort()
    {
        SortGlyph = _sortByTime ? "\uE823" : "\uE8CB";
        SortText = _sortByTime ? "최신순" : "이름순";
        _settings.IsFriendsListSortedByTime = _sortByTime;
        ApplyFilterAndSort(Query);
    }

    private void ApplyFilterAndSort(string query)
    {
        if (_items == null) return;

        IEnumerable<BaseFriendshipViewModel> viewModels = _items;
        if (!string.IsNullOrWhiteSpace(query))
        {
            viewModels = viewModels.Where(x => x.Nickname.Contains(query, StringComparison.OrdinalIgnoreCase) || KoreanHelper.SplitToChosung(x.Nickname).Contains(query, StringComparison.OrdinalIgnoreCase) || (x is HistoryFriendshipViewModel historyFriendshipViewModel && historyFriendshipViewModel.User.Handle.Contains(query, StringComparison.OrdinalIgnoreCase)));
        }

        if (_sortByTime) viewModels = viewModels.OrderByDescending(x => (x as HistoryFriendshipViewModel)?.CreatedAt ?? DateTime.MinValue);
        else viewModels = viewModels.OrderByDescending(x => x.IsFavorite).ThenBy(x => x.Nickname);

        Items = new(viewModels);
    }
}
