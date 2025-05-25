using History.MobileClient.Enums;
using History.MobileClient.ViewModels;

namespace History.MobileClient.Pages;

public partial class PostInteractionsPage : TabbedPage
{
    public PostInteractionsPage(IEnumerable<PostInteractionViewModel> viewModels, PostInteractionType type)
    {
        InitializeComponent();
        ReactionTab.SetUsers(viewModels.Where(x => x.Type == PostInteractionType.Reaction).Select(x => new FriendshipViewModel(x.User, x)), PostInteractionType.Reaction);
        ShareTab.SetUsers(viewModels.Where(x => x.Type == PostInteractionType.Share).Select(x => new FriendshipViewModel(x.User, x)), PostInteractionType.Share);
        RepostTab.SetUsers(viewModels.Where(x => x.Type == PostInteractionType.Repost).Select(x => new FriendshipViewModel(x.User, x)), PostInteractionType.Repost);

        SelectedItem = type switch
        {
            PostInteractionType.Reaction => ReactionTab,
            PostInteractionType.Share => ShareTab,
            PostInteractionType.Repost => RepostTab,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    protected override bool OnBackButtonPressed()
    {
        _ = App.PopModalAsync();
        return true;
    }
}
