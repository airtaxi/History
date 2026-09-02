using History.Commons;
using History.Commons.DataTypes.Contents;
using Microsoft.UI.Xaml.Media.Imaging;

namespace History.WindowsClient.ViewModels;

// Sticker picker grid item: wraps a sticker asset and carries the StickerContent
// the comment editor consumes when the asset is selected.
// GIF/WebP animation is not supported by BitmapImage; only the first frame is shown.
public sealed partial class StickerPickerAssetViewModel(StickerContent stickerContent)
{
    public StickerContent StickerContent { get; } = stickerContent;

    public BitmapImage ImageSource => StickerContent.StickerMediaId == null ? null : new BitmapImage(new Uri(CommonUtils.GenerateMediaUri(StickerContent.StickerMediaId)));
}