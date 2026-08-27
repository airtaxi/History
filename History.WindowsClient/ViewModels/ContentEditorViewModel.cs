using CommunityToolkit.Mvvm.ComponentModel;
using History.Commons;
using History.Commons.Helpers;
using History.WindowsClient.ViewModels;

namespace History.WindowsClient.ViewModels;

public partial class ContentEditorViewModel : ObservableObject
{
    [ObservableProperty]
    public partial bool IsKakaoMentionMode { get; set; }

    // When true, @-mention suggestions use the logged-in Kakao Story friends (CommonShared.KakaoFriends)
    // instead of the History friends (CommonShared.Friends).
    public List<MentionUserViewModel> GetUserSuggestions(string query)
    {
        query = query?.Trim() ?? string.Empty;
        return IsKakaoMentionMode ? BuildKakaoMentionViewModels(query) : BuildHistoryMentionViewModels(query);
    }

    public List<string> GetHashtagSuggestions(string query)
    {
        query = query?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(query)) return null;

        return [query];
    }

    private static List<MentionUserViewModel> BuildHistoryMentionViewModels(string query)
    {
        if (string.IsNullOrEmpty(query)) return [.. CommonShared.Friends.Select(friendUser => new MentionUserViewModel(friendUser))];

        return [.. CommonShared.Friends
            .Where(friendUser => friendUser.Handle.Contains(query, StringComparison.InvariantCultureIgnoreCase) || friendUser.Nickname.Contains(query, StringComparison.OrdinalIgnoreCase) || KoreanHelper.SplitToChosung(friendUser.Nickname).Contains(query, StringComparison.OrdinalIgnoreCase))
            .Select(friendUser => new MentionUserViewModel(friendUser))];
    }

    private static List<MentionUserViewModel> BuildKakaoMentionViewModels(string query)
    {
        if (string.IsNullOrEmpty(query)) return [.. CommonShared.KakaoFriends.Select(profile => new MentionUserViewModel(profile))];

        return [.. CommonShared.KakaoFriends
            .Where(profile => profile.display_name != null && (profile.display_name.Contains(query, StringComparison.OrdinalIgnoreCase) || KoreanHelper.SplitToChosung(profile.display_name).Contains(query, StringComparison.OrdinalIgnoreCase)))
            .Select(profile => new MentionUserViewModel(profile))];
    }
}