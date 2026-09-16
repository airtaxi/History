using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using History.Commons;
using History.Commons.DataTypes.ResponseDtos;
using Microsoft.UI.Xaml.Media.Imaging;

namespace History.WindowsClient.ViewModels.Sticker;

// Sticker detail asset tile: wraps a single sticker asset and forwards the tap to the detail
// window view model, which shows the asset in the in-window preview overlay.
public sealed partial class StickerAssetItemViewModel(StickerAssetResponseDto asset, StickerDetailWindowViewModel windowViewModel) : ObservableObject
{
    public BitmapImage ImageSource => string.IsNullOrEmpty(asset.MediaId) ? null : new BitmapImage(new Uri(CommonUtils.GenerateMediaUri(asset.MediaId)));

    [RelayCommand]
    private void HandleTap() => windowViewModel.ShowAssetPreview(this);
}
