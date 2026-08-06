using History.MobileClient.Enums;
using History.MobileClient.KakaoStory;
using static History.MobileClient.KakaoStory.KakaoStoryApiHandler.DataType;

namespace History.MobileClient.ViewModels;

public partial class KakaoInteractionViewModel : BaseInteractionViewModel
{
    public KakaoInteractionViewModel(ShareData.Share share)
    {
        Type = InteractionType.Reaction;
        CreatedAt = share.created_at;
        TargetPostId = null;
        ReactionType = null;

        ProfileMedia = share.actor?.profile_image_url != null ? new ImageViewModel(share.actor.profile_image_url) : null;
        FontFamily = "FASolid";
        var visual = KakaoStoryUtils.GetEmotionVisual(share.emotion);
        Glyph = visual.Glyph;
        Color = visual.Color;
    }

    public override async Task HandleTapAsync() { }
}
