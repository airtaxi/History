using History.Commons.Enums;
using History.WindowsClient.Helpers;
using History.WindowsClient.Models;
using History.WindowsClient.Pages;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.UI;
using static History.Commons.KakaoStory.KakaoStoryApiHandler.DataType;

namespace History.WindowsClient.ViewModels;

// Interaction entry for a Kakao Story like/share/UP. The glyph and color palette follows
// the History reaction visuals so the shared interaction template renders both identically.
public partial class KakaoInteractionViewModel : BaseInteractionViewModel
{
    private readonly BaseViewModel _baseViewModel;

    public ShareData.Share Share { get; }

    public KakaoInteractionViewModel(ShareData.Share share, BaseViewModel baseViewModel, InteractionType type = InteractionType.Reaction)
    {
        _baseViewModel = baseViewModel;
        Share = share;
        Type = type;
        CreatedAt = share.created_at;
        // Only shares carry the shared post id for navigation.
        TargetPostId = type == InteractionType.Share ? share.activity_id : null;
        ReactionType = type == InteractionType.Reaction ? MapEmotionToReactionType(share.emotion) : null;

        ProfileImageSource = share.actor?.profile_image_url != null ? new BitmapImage(new Uri(share.actor.profile_image_url)) : null;

        if (type == InteractionType.Share)
        {
            Glyph = "\uE72D";
            ColorBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x65, 0x52, 0xDF));
        }
        else if (type == InteractionType.Repost)
        {
            Glyph = "\uE8EB";
            ColorBrush = new SolidColorBrush(Color.FromArgb(0xFF, 0x99, 0x99, 0x99));
        }
        else
        {
            var visual = KakaoStoryUtils.GetEmotionVisual(share.emotion);
            Glyph = visual.Glyph;
            ColorBrush = new SolidColorBrush(visual.Color);
        }
    }

    // Kakao Story emotions map to the History reaction types.
    private static ReactionType? MapEmotionToReactionType(string emotion) => emotion switch
    {
        "like" => Commons.Enums.ReactionType.Like,
        "good" => Commons.Enums.ReactionType.Awesome,
        "pleasure" => Commons.Enums.ReactionType.Happy,
        "sad" => Commons.Enums.ReactionType.Sad,
        "cheerup" => Commons.Enums.ReactionType.Support,
        _ => null,
    };

    // Opens the acting user's profile.
    public override void HandleTap()
    {
        if (Share.actor?.id == null) return;

        _baseViewModel.RequestNavigation(typeof(ProfilePage), new KakaoProfileParameters(Share.actor.id));
    }
}
