using History.Commons;
using History.Commons.Helpers;
using History.Commons.KakaoStory;
using History.WindowsClient.Helpers;
using Microsoft.UI.Xaml;
using System.Collections.ObjectModel;

namespace History.WindowsClient.ViewModels;

// Kakao Story write-message dialog: the receiver list comes from the Kakao Story friends
// cache and the composed mail is sent through the Kakao Story message API.
public partial class KakaoWriteMessageDialogViewModel(BaseViewModel baseViewModel) : HistoryWriteMessageDialogViewModel(baseViewModel)
{
    public override bool IsAttachmentAvailable => false;

    protected override void PopulateDefaultSuggestions()
    {
        if (CommonShared.KakaoFriends == null)
        {
            SuggestedFriends = [];
            return;
        }

        SuggestedFriends = new(CommonShared.KakaoFriends.OrderBy(x => x.display_name).Select(x => (BaseFriendshipViewModel)new KakaoFriendshipViewModel(x, BaseViewModel) { FriendshipVisibility = Visibility.Collapsed, IsRootButtonHitTestVisible = false }));
    }

    public override void FilterFriends(string query)
    {
        if (CommonShared.KakaoFriends == null)
        {
            SuggestedFriends.Clear();
            return;
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            PopulateDefaultSuggestions();
            return;
        }

        var filtered = CommonShared.KakaoFriends.Where(x => x.display_name != null && (x.display_name.Contains(query, StringComparison.OrdinalIgnoreCase) || KoreanHelper.SplitToChosung(x.display_name).Contains(query, StringComparison.OrdinalIgnoreCase))).OrderBy(x => x.display_name);

        SuggestedFriends = new(filtered.Select(x => (BaseFriendshipViewModel)new KakaoFriendshipViewModel(x, BaseViewModel) { FriendshipVisibility = Visibility.Collapsed, IsRootButtonHitTestVisible = false }));
    }

    protected override async Task<Result> SendContentsAsync(string text) => await KakaoStoryUtils.SendMailAsync(BaseViewModel, ReceiverId, text);
}
