using History.Commons.DataTypes.Contents;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;

namespace History.MobileClient.ViewModels;

public partial class MentionStickerViewModel(StickerContent stickerContent)
{
    public StickerContent StickerContent => stickerContent;
    public string StickerId => stickerContent.StickerId;
    public string StickerContentId => stickerContent.StickerContentId;

    public IMediaViewModel Media => new ImageViewModel(Utils.GenerateMediaUri(stickerContent.StickerMediaId))
    {
        IsAnimated = stickerContent.IsAnimated
    };
}
