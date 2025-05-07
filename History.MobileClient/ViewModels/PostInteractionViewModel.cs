using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using History.MobileClient.Enums;
using History.MobileClient.Pages;
using UraniumUI.Icons.FontAwesome;
using UraniumUI.Icons.MaterialSymbols;

namespace History.MobileClient.ViewModels;

public partial class PostInteractionViewModel
{
    public PostInteractionType Type { get; }
    public DateTime CreatedAt { get; }
    public UserResponseDto User { get; }
    public string TargetPostId { get; }
    public PostReactionType? ReactionType { get; }


    public double IconSize { get; } = 12;
    public IMediaViewModel ProfileMedia { get; }
    public string FontFamily { get; }
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
        FontFamily = "FASolid";
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

        if (reaction.Type == PostReactionType.Like) IconSize = 9;
    }

    public PostInteractionViewModel(SharedAndRepostedUserDto sharedUser, bool isShare)
    {
        Type = PostInteractionType.Share;
        CreatedAt = sharedUser.SharedAt;
        User = sharedUser.User;
        TargetPostId = !sharedUser.IsRepost ? sharedUser.PostId : null;

        ProfileMedia = sharedUser.User.UsesAnimatedProfileMedia
        ? new VideoViewModel(Utils.GenerateMediaUri(sharedUser.User.ProfileMediaId))
        : new ImageViewModel(Utils.GenerateMediaUri(sharedUser.User.ProfileMediaId) ?? Constants.DefaultProfileImageFileName);
        FontFamily = "MaterialSharp";
        Glyph = isShare ? MaterialSharp.Share : MaterialSharp.Shift_lock;
        Color = isShare ? Color.FromRgb(0x65, 0x52, 0xdf) : Color.FromRgb(0x99, 0x99, 0x99);
    }

    [RelayCommand]
    private async Task HandleTapAsync()
    {
        var userPage = new UserPage(User.UserId);
        await App.PushModalAsync(userPage);
    }
}
