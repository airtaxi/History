using CommunityToolkit.Mvvm.Input;
using History.Commons;
using History.Commons.DataTypes.Contents;
using History.WindowsClient.Views;
using Microsoft.UI.Xaml.Media.Imaging;

namespace History.WindowsClient.ViewModels;

// Wraps a single sticker content for the inline sticker image slot and opens the sticker
// detail window when the sticker is tapped. Kakao Story emoticons carry no sticker id, so
// their detail is reported as unsupported instead.
// GIF/WebP animation is not supported by BitmapImage; only the first frame is shown.
public sealed partial class StickerContentItemViewModel(StickerContent stickerContent, BaseViewModel baseViewModel) : IContentViewModel
{
    public string StickerId => stickerContent.StickerId;

    public BitmapImage ImageSource => stickerContent.StickerMediaId == null ? null : new BitmapImage(new Uri(CommonUtils.GenerateMediaUri(stickerContent.StickerMediaId)));

    [RelayCommand]
    private async Task HandleTapAsync()
    {
        if (string.IsNullOrEmpty(StickerId))
        {
            await StickerDetailWindow.ShowEmoticonNoticeAsync(baseViewModel);
            return;
        }

        StickerDetailWindow.ShowModal(StickerId, MainWindow.Instance);
    }
}
