using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using History.MobileClient.Pages;
using UraniumUI.Icons.FontAwesome;
using UraniumUI.Icons.MaterialSymbols;
using ReactionEnum = History.Commons.Enums.ReactionType;

namespace History.MobileClient.ViewModels;

public partial class HistoryInteractionViewModel : BaseInteractionViewModel
{
    public UserResponseDto User { get; }

    public HistoryInteractionViewModel(PostReactionDto reaction)
    {
        Type = InteractionType.Reaction;
        CreatedAt = reaction.CreatedAt;
        User = reaction.User;
        ReactionType = reaction.Type;

        ProfileMedia = reaction.User.UsesAnimatedProfileMedia
            ? new ImageViewModel(Utils.GenerateMediaUri(reaction.User.ProfileMediaId) ?? Constants.DefaultProfileImageFileName) { IsAnimated = true }
            : new ImageViewModel(Utils.GenerateMediaUri(reaction.User.ProfileMediaId) ?? Constants.DefaultProfileImageFileName);
        FontFamily = "FASolid";
        Glyph = reaction.Type switch
        {
            ReactionEnum.Like => Solid.Heart,
            ReactionEnum.Awesome => Solid.Star,
            ReactionEnum.Happy => Solid.FaceSmile,
            ReactionEnum.Sad => Solid.Droplet,
            ReactionEnum.Support => Solid.Bolt,
            _ => throw new ArgumentOutOfRangeException(nameof(reaction.Type), reaction.Type, null)
        };
        Color = reaction.Type switch
        {
            ReactionEnum.Like => Color.FromRgb(0xeb, 0x55, 0x27),
            ReactionEnum.Awesome => Color.FromRgb(0xbb, 0xcc, 0x29),
            ReactionEnum.Happy => Color.FromRgb(0xff, 0xc1, 0x00),
            ReactionEnum.Sad => Color.FromRgb(0x00, 0x9f, 0xb2),
            ReactionEnum.Support => Color.FromRgb(0xa0, 0x61, 0xb1),
            _ => throw new ArgumentOutOfRangeException(nameof(reaction.Type), reaction.Type, null)
        };

        if (reaction.Type == ReactionEnum.Like) IconSize = 9;
    }

    // Comment Like
    public HistoryInteractionViewModel(UserResponseDto user)
    {
        Type = InteractionType.CommentLike;
        CreatedAt = DateTime.UtcNow; // Comment likes do not have a created date in the API
        User = user;
        ReactionType = ReactionEnum.Like;

        ProfileMedia = user.UsesAnimatedProfileMedia
            ? new ImageViewModel(Utils.GenerateMediaUri(user.ProfileMediaId) ?? Constants.DefaultProfileImageFileName) { IsAnimated = true }
            : new ImageViewModel(Utils.GenerateMediaUri(user.ProfileMediaId) ?? Constants.DefaultProfileImageFileName);

        FontFamily = "FASolid";
        Glyph = Solid.Heart;
        Color = Color.FromRgb(0xeb, 0x55, 0x27);
    }

    public HistoryInteractionViewModel(SharedAndRepostedUserDto sharedUser, bool isShare)
    {
        Type = sharedUser.IsRepost ? InteractionType.Repost : InteractionType.Share;
        CreatedAt = sharedUser.SharedAt;
        User = sharedUser.User;
        TargetPostId = !sharedUser.IsRepost ? sharedUser.PostId : null;

        ProfileMedia = sharedUser.User.UsesAnimatedProfileMedia
            ? new ImageViewModel(Utils.GenerateMediaUri(sharedUser.User.ProfileMediaId) ?? Constants.DefaultProfileImageFileName) { IsAnimated = true }
            : new ImageViewModel(Utils.GenerateMediaUri(sharedUser.User.ProfileMediaId) ?? Constants.DefaultProfileImageFileName);
        FontFamily = "MaterialSharp";
        Glyph = isShare ? MaterialSharp.Share : MaterialSharp.Shift_lock;
        Color = isShare ? Color.FromRgb(0x65, 0x52, 0xdf) : Color.FromRgb(0x99, 0x99, 0x99);
    }

    public override async Task HandleTapAsync()
    {
        var userPage = new BlazorUserPage(User.UserId);
        await App.PushAsync(userPage);
    }
}
