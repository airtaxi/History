using CommunityToolkit.Mvvm.ComponentModel;
using History.Commons;
using History.Commons.Helpers;
using History.WindowsClient.ViewModels;
using Microsoft.UI.Xaml;

namespace History.WindowsClient.ViewModels;

public partial class ContentEditorViewModel(BaseViewModel baseViewModel) : ObservableObject
{
    [ObservableProperty]
    public partial bool IsKakaoMentionMode { get; set; }

    // When true, @-mention suggestions use the logged-in Kakao Story friends (CommonShared.KakaoFriends)
    // instead of the History friends (CommonShared.Friends).
    public List<BaseFriendshipViewModel> GetUserSuggestions(string query)
    {
        query = query?.Trim() ?? string.Empty;
        return BuildHistoryMentionViewModels(query);
        //return IsKakaoMentionMode ? BuildKakaoMentionViewModels(query) : BuildHistoryMentionViewModels(query);
    }

    public List<string> GetHashtagSuggestions(string query)
    {
        query = query?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(query)) return null;

        return [query];
    }

    private List<BaseFriendshipViewModel> BuildHistoryMentionViewModels(string query)
    {
        var orderedFriends = CommonShared.Friends.OrderByDescending(x => x.IsFavorite).ThenBy(x => x.Nickname);

        if (string.IsNullOrEmpty(query)) return [.. orderedFriends.Select(friendUser => new HistoryFriendshipViewModel(friendUser, baseViewModel) { FriendshipVisibility = Visibility.Collapsed })];
        else return [.. orderedFriends
            .Where(friendUser => friendUser.Handle.Contains(query, StringComparison.InvariantCultureIgnoreCase) || friendUser.Nickname.Contains(query, StringComparison.OrdinalIgnoreCase) || KoreanHelper.SplitToChosung(friendUser.Nickname).Contains(query, StringComparison.OrdinalIgnoreCase))
            .Select(friendUser => new HistoryFriendshipViewModel(friendUser, baseViewModel) { FriendshipVisibility = Visibility.Collapsed })];
    }

    //private List<BaseFriendshipViewModel> BuildKakaoMentionViewModels(string query)
    //{
    //    if (string.IsNullOrEmpty(query)) return [.. CommonShared.KakaoFriends.Select(profile => new KakaoFriendshipViewModel(profile, baseViewModel))];

    //    return [.. CommonShared.KakaoFriends
    //        .Where(profile => profile.display_name != null && (profile.display_name.Contains(query, StringComparison.OrdinalIgnoreCase) || KoreanHelper.SplitToChosung(profile.display_name).Contains(query, StringComparison.OrdinalIgnoreCase)))
    //        .Select(profile => new KakaoFriendshipViewModel(profile, baseViewModel))];
    //}
}