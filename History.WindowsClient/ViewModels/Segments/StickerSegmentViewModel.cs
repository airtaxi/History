using History.Commons;
using History.Commons.DataTypes.Contents;

namespace History.WindowsClient.ViewModels.Segments;

public sealed record StickerSegmentViewModel(StickerContent Sticker) : BodyContentSegmentViewModel
{
    public string ImageUri => CommonUtils.GenerateMediaUri(Sticker.StickerMediaId);
}