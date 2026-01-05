using History.Commons;
using History.Commons.DataTypes.Contents;

namespace History.MobileClient.ViewModels;

public class StickerContentViewModel(StickerContent stickerContent) : IContentViewModel
{
    public string StickerId => stickerContent.StickerId;
    public string StickerContentId => stickerContent.StickerContentId;
    public ImageViewModel Media { get; } = new ImageViewModel(Utils.GenerateMediaUri(stickerContent.StickerMediaId))
    {
        HorizontalContentOptions = LayoutOptions.Fill,
        VerticalContentOptions = LayoutOptions.Fill,
        Aspect = Aspect.AspectFit
    };
}
