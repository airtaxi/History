using History.MobileClient.Enums;
using History.MobileClient.KakaoStory;
using UraniumUI.Icons.MaterialSymbols;
using static History.MobileClient.KakaoStory.KakaoStoryApiHandler.DataType;

namespace History.MobileClient.ViewModels;

public partial class KakaoInteractionViewModel : BaseInteractionViewModel
{
    public KakaoInteractionViewModel(ShareData.Share share, InteractionType type = InteractionType.Reaction)
    {
        Type = type;
        CreatedAt = share.created_at;
        TargetPostId = null;
        ReactionType = null;

        ProfileMedia = share.actor?.profile_image_url != null ? new ImageViewModel(share.actor.profile_image_url) : null;

        if (type == InteractionType.Share)
        {
            FontFamily = "MaterialSharp";
            Glyph = MaterialSharp.Share;
            Color = Color.FromRgb(0x65, 0x52, 0xdf);
        }
        else if (type == InteractionType.Repost)
        {
            FontFamily = "MaterialSharp";
            Glyph = MaterialSharp.Shift_lock;
            Color = Color.FromRgb(0x99, 0x99, 0x99);
        }
        else
        {
            FontFamily = "FASolid";
            var visual = KakaoStoryUtils.GetEmotionVisual(share.emotion);
            Glyph = visual.Glyph;
            Color = visual.Color;
        }
    }

    public override async Task HandleTapAsync() { }
}
