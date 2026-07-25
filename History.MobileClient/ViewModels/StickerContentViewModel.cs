using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using History.Commons;
using History.Commons.DataTypes.Contents;
using History.MobileClient.Pages;

namespace History.MobileClient.ViewModels;

public partial class StickerContentViewModel(StickerContent stickerContent) : ObservableObject, IContentViewModel
{
    public string StickerId => stickerContent.StickerId;
    public string StickerContentId => stickerContent.StickerContentId;
    public ImageViewModel Media { get; } = new ImageViewModel(Utils.GenerateMediaUri(stickerContent.StickerMediaId))
    {
        HorizontalContentOptions = LayoutOptions.Fill,
        VerticalContentOptions = LayoutOptions.Fill,
        Aspect = Aspect.AspectFit,
        IsAnimated = stickerContent.IsAnimated
    };

    [RelayCommand]
    private async Task NavigateToStickerDetailAsync()
    {
        if (string.IsNullOrEmpty(StickerId)) return;

        var result = await App.ExecuteRequestAsync(new Commons.Api.Sticker.GetSticker(StickerId));
        if (result.IsSuccess)
        {
            await App.PushAsync(new StickerDetailPage(result.Value));
        }
    }
}
