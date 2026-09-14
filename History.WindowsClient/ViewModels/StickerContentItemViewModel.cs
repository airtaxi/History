using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using History.Commons.DataTypes.Contents;
using History.WindowsClient.Helpers;
using History.WindowsClient.Views;
using Microsoft.UI.Xaml.Media.Imaging;

namespace History.WindowsClient.ViewModels;

// Wraps a single sticker content for the inline sticker image slot and opens the sticker
// detail window when the sticker is tapped. Kakao Story emoticons carry no sticker id, so
// their detail is reported as unsupported instead. Kakao Story emoticon images are
// hotlink-protected, so the image source loads asynchronously through the Referer-aware loader.
// GIF/WebP animation is not supported by BitmapImage; only the first frame is shown.
public sealed partial class StickerContentItemViewModel : ObservableObject, IContentViewModel
{
    private readonly BaseViewModel _baseViewModel;

    public StickerContentItemViewModel(StickerContent stickerContent, BaseViewModel baseViewModel)
    {
        _baseViewModel = baseViewModel;
        StickerId = stickerContent.StickerId;
        _ = LoadImageAsync(stickerContent.StickerMediaId);
    }

    public string StickerId { get; }

    [ObservableProperty]
    public partial BitmapImage ImageSource { get; private set; }

    [RelayCommand]
    private async Task HandleTapAsync()
    {
        if (string.IsNullOrEmpty(StickerId))
        {
            await StickerDetailWindow.ShowEmoticonNoticeAsync(_baseViewModel);
            return;
        }

        StickerDetailWindow.ShowModal(StickerId, MainWindow.Instance);
    }

    // A failed load leaves ImageSource null; the slot then renders empty.
    private async Task LoadImageAsync(string stickerMediaId)
    {
        if (stickerMediaId == null) return;

        ImageSource = await KakaoEmoticonImageLoader.CreateImageSourceAsync(stickerMediaId);
    }
}
