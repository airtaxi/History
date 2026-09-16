using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Media.Imaging;

namespace History.WindowsClient.ViewModels.Sticker;

// Draft sticker asset added in the create window: keeps the picked file bytes for the upload
// payload and the decoded preview thumbnail, and forwards the tile removal to the create
// window view model.
public sealed partial class CreateStickerAssetItemViewModel(string fileName, byte[] data, BitmapImage imageSource, CreateStickerWindowViewModel windowViewModel) : ObservableObject
{
    public string FileName { get; } = fileName;

    public byte[] Data { get; } = data;

    public BitmapImage ImageSource { get; } = imageSource;

    [RelayCommand]
    private void Delete() => windowViewModel.RemoveAsset(this);
}
