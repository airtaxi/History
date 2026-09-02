using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using History.Commons;
using History.Commons.Api.Friendship;
using History.Commons.Api.User;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Helpers;
using History.WindowsClient.Messages;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;

namespace History.WindowsClient.ViewModels.MainPage;

public partial class MainPageFriendshipSideBarAddFriendsItemViewModel : BaseMainPageFriendshipSideBarItemViewModel, IRecipient<FriendshipChangedMessage>
{
    private long _searchSequence;

    public MainPageFriendshipSideBarAddFriendsItemViewModel(MainPageViewModel hostViewModel) : base(hostViewModel)
    {
        if (!HostViewModel.IsKakaoStoryMode) SearchAutoSuggestBoxPlaceholderText = "친구의 닉네임 또는 핸들 검색";
        else SearchAutoSuggestBoxPlaceholderText = "카카오스토리 ID 검색";

        Query = string.Empty;
        RightHeaderText = "검색 결과";
        EmptyText = "검색 결과가 없습니다";
        IsEmpty = true;

        WeakReferenceMessenger.Default.Register(this);
    }

    public void Receive(FriendshipChangedMessage message)
    {
        if (HostViewModel.IsKakaoStoryMode) return; // Kakao Story friends are not tracked by the History friendship message.
        if (string.IsNullOrWhiteSpace(Query)) return; // No search results are shown yet.

        ApplyQuery(Query);
    }

    // RefreshAsync is no-op for add frinnds item
    public override async Task RefreshAsync() { }

    // TextChanged is no-op for add frinnds item
    public override void OnFriendshipSideBarAutoSuggestBoxTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args) { }
    public override void OnFriendshipSideBarAutoSuggestBoxQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args) => ApplyQuery(sender.Text);

    private async void ApplyQuery(string query)
    {
        var sequence = ++_searchSequence;
        var isKakaoStoryMode = HostViewModel.IsKakaoStoryMode;
        var viewModels = new List<BaseFriendshipViewModel>();

        if (!HostViewModel.IsKakaoStoryMode)
        {
            var results = new List<UserResponseDto>();

            // Add handle results
            var handleResult = await App.ExecuteRequestAsync(new GetUserByHandle(query), [ErrorType.NotFound, ErrorType.Forbidden]);
            if (handleResult.IsSuccess) results.Add(handleResult);

            // Add nickname results
            var nicknameResults = await App.ExecuteRequestAsync(new FindUsersByNickname(query));
            if (nicknameResults.IsSuccess) results.AddRange(nicknameResults.Value);

            // The mode can change while the search runs (fast pill switching); discard the stale result, the pending switch reloads.
            if (isKakaoStoryMode != HostViewModel.IsKakaoStoryMode) return;

            // Remove myself from results
            results.RemoveAll(x => x.UserId == CommonShared.UserId);

            // Delete duplicated records
            results = [.. results.DistinctBy(x => x.UserId)];

            viewModels = [.. results.Select(x => new HistoryFriendshipViewModel(x, HostViewModel))];
        }

        if (sequence != _searchSequence) return; // A newer search was issued; discard stale results.

        Items = [.. viewModels];
        IsEmpty = viewModels.Count == 0;
    }
}
