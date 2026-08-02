using CommunityToolkit.Mvvm.Input;
using History.Commons.DataTypes.ResponseDtos;
using History.Uno.Enums;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace History.Uno.ViewModels;

public partial class InteractionViewModel
{
    public InteractionType Type { get; }
    public DateTime CreatedAt { get; }
    public UserResponseDto User { get; }
    public string TargetPostId { get; }
    public History.Commons.Enums.ReactionType? ReactionType { get; }

    public double IconSize { get; } = 12;
    public IMediaViewModel ProfileMedia { get; }
    public string Glyph { get; }
    public SolidColorBrush Brush { get; }

    public InteractionViewModel(PostReactionDto reaction)
    {
        Type = InteractionType.Reaction;
        CreatedAt = reaction.CreatedAt;
        User = reaction.User;
        ReactionType = reaction.Type;

        ProfileMedia = GenerateUserMedia(reaction.User);
        (Glyph, Brush) = reaction.Type switch
        {
            History.Commons.Enums.ReactionType.Like => ("\uEB52", CreateBrush(0xEB, 0x55, 0x27)),          // HeartFill
            History.Commons.Enums.ReactionType.Awesome => ("\uE735", CreateBrush(0xBB, 0xCC, 0x29)),       // FavoriteStarFill
            History.Commons.Enums.ReactionType.Happy => ("\uE76E", CreateBrush(0xFF, 0xC1, 0x00)),         // Emoji2
            History.Commons.Enums.ReactionType.Sad => ("\uEB42", CreateBrush(0x00, 0x9F, 0xB2)),           // Drop
            History.Commons.Enums.ReactionType.Support => ("\uE945", CreateBrush(0xA0, 0x61, 0xB1)),       // LightningBolt
            _ => throw new ArgumentOutOfRangeException(nameof(reaction.Type), reaction.Type, null)
        };

        if (reaction.Type == History.Commons.Enums.ReactionType.Like) IconSize = 9;
    }

    // Comment Like
    public InteractionViewModel(UserResponseDto user)
    {
        Type = InteractionType.CommentLike;
        CreatedAt = DateTime.UtcNow; // Comment likes do not have a created date in the API
        User = user;
        ReactionType = History.Commons.Enums.ReactionType.Like;

        ProfileMedia = GenerateUserMedia(user);
        Glyph = "\uEB52"; // HeartFill
        Brush = CreateBrush(0xEB, 0x55, 0x27);
    }

    public InteractionViewModel(SharedAndRepostedUserDto sharedUser, bool isShare)
    {
        Type = sharedUser.IsRepost ? InteractionType.Repost : InteractionType.Share;
        CreatedAt = sharedUser.SharedAt;
        User = sharedUser.User;
        TargetPostId = !sharedUser.IsRepost ? sharedUser.PostId : null;

        ProfileMedia = GenerateUserMedia(sharedUser.User);
        Glyph = isShare ? "\uE72D" : "\uE8EB"; // Share / Reshare
        Brush = isShare ? CreateBrush(0x65, 0x52, 0xDF) : CreateBrush(0x99, 0x99, 0x99);
    }

    [RelayCommand]
    private async Task HandleTapAsync() => await App.PushAsync(typeof(Pages.UserPage), User.UserId);

    // Animated WebP profile media plays automatically in Uno Image, so no IsAnimated flag is needed.
    private static ImageViewModel GenerateUserMedia(UserResponseDto user) => new(Utils.GenerateMediaUri(user.ProfileMediaId) ?? Constants.DefaultProfileImageFileName);

    private static SolidColorBrush CreateBrush(byte red, byte green, byte blue) => new(Color.FromArgb(0xFF, red, green, blue));
}
