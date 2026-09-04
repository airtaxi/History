using History.Commons;
using History.Commons.Api.Friendship;
using History.Commons.Helpers;

namespace History.WindowsClient.ViewModels.DiscoveryOptions;

// History implementation of discovery option user selection view model.
public partial class HistoryDiscoveryOptionSelectUsersViewModel(IReadOnlyList<string> initialSelectedUserIds, BaseViewModel baseViewModel) : BaseDiscoveryOptionSelectUsersViewModel(baseViewModel)
{
    private readonly IReadOnlyList<string> _initialSelectedUserIds = initialSelectedUserIds ?? [];

    public override async Task InitializeAsync()
    {
        if (IsLoading) return;
        IsLoading = true;

        try
        {
            var result = await BaseViewModel.ExecuteRequestAsync(new GetFriends(CommonShared.UserId));
            if (result.IsSuccess)
            {
                var viewModels = result.Value.OrderByDescending(user => user.IsFavorite).ThenBy(user => user.Nickname)
                    .Select(user => new HistorySelectUserViewModel(user, _initialSelectedUserIds.Contains(user.UserId)))
                    .ToList();

                SetAllUsers(viewModels);
            }
            else SetAllUsers([]);
        }
        finally { IsLoading = false; }
    }

    protected override bool MatchesFilter(BaseSelectUserViewModel user, string query)
    {
        if (user.Nickname.Contains(query, StringComparison.OrdinalIgnoreCase)) return true;
        if (KoreanHelper.SplitToChosung(user.Nickname).Contains(query, StringComparison.OrdinalIgnoreCase)) return true;
        if (user is HistorySelectUserViewModel historyUser && historyUser.User.Handle != null && historyUser.User.Handle.Contains(query, StringComparison.OrdinalIgnoreCase)) return true;

        return false;
    }
}
