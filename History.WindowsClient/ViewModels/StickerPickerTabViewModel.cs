using CommunityToolkit.Mvvm.ComponentModel;
using History.Commons;
using History.Commons.DataTypes.ResponseDtos;
using Microsoft.UI.Xaml.Media.Imaging;

namespace History.WindowsClient.ViewModels;

// Sticker picker tab bar item: the recent-usage tab or one available sticker.
// The recent tab carries no sticker and shows a clock icon instead.
public sealed partial class StickerPickerTabViewModel(StickerResponseDto sticker, bool isRecent) : ObservableObject
{
    public StickerResponseDto Sticker { get; } = sticker;

    public bool IsRecent { get; } = isRecent;

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    public BitmapImage IconSource => IsRecent || Sticker?.IconMediaId == null ? null : new BitmapImage(new Uri(CommonUtils.GenerateMediaUri(Sticker.IconMediaId)));
}