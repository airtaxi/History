using History.Commons;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.UI;
using ReactionEnum = History.Commons.Enums.ReactionType;

namespace History.WindowsClient.ViewModels;

// Reaction glyphs use the Segoe Fluent glyphs provided by the project owner,
// and the reaction colors are fixed palette values.
public partial class HistoryInteractionViewModel : BaseInteractionViewModel
{
    public UserResponseDto User { get; }

    public HistoryInteractionViewModel(PostReactionDto reaction)
    {
        Type = InteractionType.Reaction;
        CreatedAt = reaction.CreatedAt;
        User = reaction.User;
        ReactionType = reaction.Type;

        ProfileImageSource = CreateProfileImageSource(reaction.User);
        Glyph = reaction.Type switch
        {
            ReactionEnum.Like => "\uEB52",
            ReactionEnum.Awesome => "\uE735",
            ReactionEnum.Happy => "\uED54",
            ReactionEnum.Sad => "\uEB42",
            ReactionEnum.Support => "\uE945",
            _ => throw new ArgumentOutOfRangeException(nameof(reaction.Type), reaction.Type, null),
        };
        ColorBrush = new SolidColorBrush(reaction.Type switch
        {
            ReactionEnum.Like => Color.FromArgb(0xFF, 0xEB, 0x55, 0x27),
            ReactionEnum.Awesome => Color.FromArgb(0xFF, 0xBB, 0xCC, 0x29),
            ReactionEnum.Happy => Color.FromArgb(0xFF, 0xFF, 0xC1, 0x00),
            ReactionEnum.Sad => Color.FromArgb(0xFF, 0x00, 0x9F, 0xB2),
            ReactionEnum.Support => Color.FromArgb(0xFF, 0xA0, 0x61, 0xB1),
            _ => throw new ArgumentOutOfRangeException(nameof(reaction.Type), reaction.Type, null),
        });

        if (reaction.Type == ReactionEnum.Like) IconSize = 9;
    }

    public HistoryInteractionViewModel(SharedAndRepostedUserDto sharedUser, bool isShare)
    {
        Type = sharedUser.IsRepost ? InteractionType.Repost : InteractionType.Share;
        CreatedAt = sharedUser.SharedAt;
        User = sharedUser.User;
        TargetPostId = !sharedUser.IsRepost ? sharedUser.PostId : null;

        ProfileImageSource = CreateProfileImageSource(sharedUser.User);
        // Share uses \uE72D, repost (reshare) uses \uE8EB.
        Glyph = isShare ? "\uE72D" : "\uE8EB";
        ColorBrush = new SolidColorBrush(isShare ? Color.FromArgb(0xFF, 0x65, 0x52, 0xDF) : Color.FromArgb(0xFF, 0x99, 0x99, 0x99));
    }

    private static BitmapImage CreateProfileImageSource(UserResponseDto user) => user.ProfileMediaId == null ? null : new BitmapImage(new Uri(CommonUtils.GenerateMediaUri(user.ProfileMediaId)));

    // TODO: Navigate to the user profile page once it is implemented.
    public override void HandleTap() { }
}
