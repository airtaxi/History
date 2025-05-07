using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using History.MobileClient.Enums;
using History.MobileClient.Pages;
using UraniumUI.Icons.FontAwesome;

namespace History.MobileClient.ViewModels;

public partial class PostInteractionViewModel
{
    public PostInteractionType Type { get; }
    public DateTime CreatedAt { get; }
    public UserResponseDto User { get; }
    public PostReactionType? ReactionType { get; }

    public IMediaViewModel ProfileMedia { get; }
    public string Glyph { get; }
    public Color Color { get; }

    public PostInteractionViewModel(PostReactionDto reaction)
    {
        Type = PostInteractionType.Reaction;
        CreatedAt = reaction.CreatedAt;
        User = reaction.User;
        ReactionType = reaction.Type;

        ProfileMedia = reaction.User.UsesAnimatedProfileMedia
        ? new VideoViewModel(Utils.GenerateMediaUri(reaction.User.ProfileMediaId))
        : new ImageViewModel(Utils.GenerateMediaUri(reaction.User.ProfileMediaId) ?? Constants.DefaultProfileImageFileName);
        Glyph = reaction.Type switch
        {
            PostReactionType.Like => Solid.Heart,
            PostReactionType.Awesome => Solid.Star,
            PostReactionType.Happy => Solid.FaceSmile,
            PostReactionType.Sad => Solid.Droplet,
            PostReactionType.Support => Solid.Bolt,
            _ => throw new ArgumentOutOfRangeException(nameof(reaction.Type), reaction.Type, null)
        };
        Color = reaction.Type switch
        {
            PostReactionType.Like => Color.FromRgb(0xeb, 0x55, 0x27),
            PostReactionType.Awesome => Color.FromRgb(0xbb, 0xcc, 0x29),
            PostReactionType.Happy => Color.FromRgb(0xbb, 0xcc, 0x29),
            PostReactionType.Sad => Color.FromRgb(0xf5, 0xbe, 0x06),
            PostReactionType.Support => Color.FromRgb(0xa0, 0x61, 0xb1),
            _ => throw new ArgumentOutOfRangeException(nameof(reaction.Type), reaction.Type, null)
        };
    }

    public PostInteractionViewModel(SharedUserDto sharedUser)
    {
        Type = PostInteractionType.Share;
        CreatedAt = sharedUser.SharedAt;
        User = sharedUser.User;

        ProfileMedia = sharedUser.User.UsesAnimatedProfileMedia
        ? new VideoViewModel(Utils.GenerateMediaUri(sharedUser.User.ProfileMediaId))
        : new ImageViewModel(Utils.GenerateMediaUri(sharedUser.User.ProfileMediaId) ?? Constants.DefaultProfileImageFileName);
        Glyph = Solid.ShareNodes;
        Color = Color.FromRgb(0x65, 0x52, 0xdf);
    }

    [RelayCommand]
    private async Task HandleTapAsync()
    {
        var userPage = new UserPage(User.UserId);
        await App.PushModalAsync(userPage);
    }
}
