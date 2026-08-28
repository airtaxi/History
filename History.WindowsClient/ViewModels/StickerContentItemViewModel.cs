using History.Commons;
using History.Commons.DataTypes.Contents;
using Microsoft.UI.Xaml.Media.Imaging;

namespace History.WindowsClient.ViewModels;

// Wraps a single sticker content for the inline sticker image slot.
// GIF/WebP animation is not supported by BitmapImage; only the first frame is shown.
public sealed partial class StickerContentItemViewModel(StickerContent stickerContent) : IContentViewModel
{
    public string StickerId => stickerContent.StickerId;

    public BitmapImage ImageSource => stickerContent.StickerMediaId == null ? null : new BitmapImage(new Uri(CommonUtils.GenerateMediaUri(stickerContent.StickerMediaId)));
}