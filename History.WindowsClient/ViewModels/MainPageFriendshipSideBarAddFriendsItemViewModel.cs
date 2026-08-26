using CommunityToolkit.Mvvm.ComponentModel;
using History.Commons;
using History.Commons.Api.Friendship;
using History.Commons.Api.User;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;

namespace History.WindowsClient.ViewModels;

public partial class MainPageFriendshipSideBarAddFriendsItemViewModel : BaseMainPageFriendshipSideBarItemViewModel
{
    private long _searchSequence;

    public MainPageFriendshipSideBarAddFriendsItemViewModel(MainPageFriendshipSideBarViewModel parentViewModel) : base(parentViewModel)
    {
        if (!Parent.Parent.IsKakaoStoryMode) SearchAutoSuggestBoxPlaceholderText = "친구의 닉네임 또는 핸들 검색";
        else SearchAutoSuggestBoxPlaceholderText = "카카오스토리 ID 검색";

        Query = string.Empty;
        RightHeaderText = "검색 결과";
        EmptyText = "검색 결과가 없습니다";
        IsEmpty = true;
    }

    // RefreshAsync is no-op for add frinnds item
    public override async Task RefreshAsync() { }

    // TextChanged is no-op for add frinnds item
    public override void OnFriendshipSideBarAutoSuggestBoxTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args) { }
    public override void OnFriendshipSideBarAutoSuggestBoxQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args) => ApplyQuery(sender.Text);

    private async void ApplyQuery(string query)
    {
        var sequence = ++_searchSequence;
        var isKakaoStoryMode = Parent.Parent.IsKakaoStoryMode;
        var viewModels = new List<BaseFriendshipViewModel>();

        if (!Parent.Parent.IsKakaoStoryMode)
        {
            var results = new List<UserResponseDto>();

            // Add handle results
            var handleResult = await App.ExecuteRequestAsync(new GetUserByHandle(query), [ErrorType.NotFound, ErrorType.Forbidden]);
            if (handleResult.IsSuccess) results.Add(handleResult);

            // Add nickname results
            var nicknameResults = await App.ExecuteRequestAsync(new FindUsersByNickname(query));
            if (nicknameResults.IsSuccess) results.AddRange(nicknameResults.Value);

            // The mode can change while the search runs (fast pill switching); discard the stale result, the pending switch reloads.
            if (isKakaoStoryMode != Parent.Parent.IsKakaoStoryMode) return;

            // Remove myself from results
            results.RemoveAll(x => x.UserId == CommonShared.UserId);

            // Delete duplicated records
            results = [.. results.DistinctBy(x => x.UserId)];

            viewModels = [.. results.Select(x => new HistoryFriendshipViewModel(x))];
        }

        if (sequence != _searchSequence) return; // A newer search was issued; discard stale results.

        Items = [.. viewModels];
        IsEmpty = viewModels.Count == 0;
    }
}
