using History.MobileClient.Pages;
using UraniumUI.Icons.MaterialSymbols;
using static History.Commons.KakaoStory.KakaoStoryApiHandler.DataType;
using History.MobileClient.KakaoStory;
using History.Commons.Enums;

namespace History.MobileClient.ViewModels;

public partial class KakaoInteractionViewModel : BaseInteractionViewModel
{
    public ShareData.Share Share { get; }

    public KakaoInteractionViewModel(ShareData.Share share, InteractionType type = InteractionType.Reaction)
    {
        Share = share;
        Type = type;
        CreatedAt = share.created_at;
        // Mirror HistoryInteractionViewModel: only shares carry the shared post id for navigation.
        TargetPostId = type == InteractionType.Share ? share.activity_id : null;
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

    public override async Task HandleTapAsync()
    {
        if (Share.actor?.id == null)
        {
            await App.Page.DisplayAlertAsync("안내", "프로필을 불러올 수 없습니다.", Constants.PromptOk);
            return;
        }

        var profilePage = new BlazorUserPage(Share.actor.id, true);
        await App.PushAsync(profilePage);
    }
}
