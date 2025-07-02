using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using History.MobileClient.Enums;
using History.MobileClient.Pages;
using UraniumUI.Icons.FontAwesome;
using UraniumUI.Icons.MaterialSymbols;

namespace History.MobileClient.ViewModels;

public partial class InteractionViewModel
{
    public InteractionType Type { get; }
    public DateTime CreatedAt { get; }
    public UserResponseDto User { get; }
    public string TargetPostId { get; }
    public ReactionType? ReactionType { get; }


    public double IconSize { get; } = 12;
    public IMediaViewModel ProfileMedia { get; }
    public string FontFamily { get; }
    public string Glyph { get; }
    public Color Color { get; }

    public InteractionViewModel(PostReactionDto reaction)
    {
        Type = InteractionType.Reaction;
        CreatedAt = reaction.CreatedAt;
        User = reaction.User;
        ReactionType = reaction.Type;

        ProfileMedia = reaction.User.UsesAnimatedProfileMedia
        ? new VideoViewModel(Utils.GenerateMediaUri(reaction.User.ProfileMediaId))
        : new ImageViewModel(Utils.GenerateMediaUri(reaction.User.ProfileMediaId) ?? Constants.DefaultProfileImageFileName);
        FontFamily = "FASolid";
        Glyph = reaction.Type switch
        {
            Commons.Enums.ReactionType.Like => Solid.Heart,
            Commons.Enums.ReactionType.Awesome => Solid.Star,
            Commons.Enums.ReactionType.Happy => Solid.FaceSmile,
            Commons.Enums.ReactionType.Sad => Solid.Droplet,
            Commons.Enums.ReactionType.Support => Solid.Bolt,
            _ => throw new ArgumentOutOfRangeException(nameof(reaction.Type), reaction.Type, null)
        };
        Color = reaction.Type switch
        {
            Commons.Enums.ReactionType.Like => Color.FromRgb(0xeb, 0x55, 0x27),
            Commons.Enums.ReactionType.Awesome => Color.FromRgb(0xbb, 0xcc, 0x29),
            Commons.Enums.ReactionType.Happy => Color.FromRgb(0xff, 0xc1, 0x00),
            Commons.Enums.ReactionType.Sad => Color.FromRgb(0x00, 0x9f, 0xb2),
            Commons.Enums.ReactionType.Support => Color.FromRgb(0xa0, 0x61, 0xb1),
            _ => throw new ArgumentOutOfRangeException(nameof(reaction.Type), reaction.Type, null)
        };

        if (reaction.Type == Commons.Enums.ReactionType.Like) IconSize = 9;
    }

    // Comment Like
    public InteractionViewModel(UserResponseDto user)
    {
        Type = InteractionType.CommentLike;
        CreatedAt = DateTime.UtcNow; // Comment likes do not have a created date in the API
        User = user;
        ReactionType = Commons.Enums.ReactionType.Like;

        ProfileMedia = user.UsesAnimatedProfileMedia
        ? new VideoViewModel(Utils.GenerateMediaUri(user.ProfileMediaId))
        : new ImageViewModel(Utils.GenerateMediaUri(user.ProfileMediaId) ?? Constants.DefaultProfileImageFileName);

        FontFamily = "FASolid";
        Glyph = Solid.Heart;
        Color = Color.FromRgb(0xeb, 0x55, 0x27);
    }

    public InteractionViewModel(SharedAndRepostedUserDto sharedUser, bool isShare)
    {
        Type = sharedUser.IsRepost ? InteractionType.Repost : InteractionType.Share;
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
        await App.PushAsync(userPage);
    }
}
