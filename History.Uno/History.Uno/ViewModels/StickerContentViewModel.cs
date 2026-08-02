using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using History.Commons.Api.Sticker;
using History.Commons.DataTypes.Contents;
using Microsoft.UI.Xaml.Media;

namespace History.Uno.ViewModels;

public partial class StickerContentViewModel(StickerContent stickerContent) : ObservableObject, IContentViewModel
{
    public string StickerId => stickerContent.StickerId;
    public string StickerContentId => stickerContent.StickerContentId;
    public ImageViewModel Media { get; } = new(Utils.GenerateMediaUri(stickerContent.StickerMediaId)) { Stretch = Stretch.Uniform };

    [RelayCommand]
    public async Task NavigateToStickerDetailAsync()
    {
        if (string.IsNullOrEmpty(StickerId)) return;

        var result = await App.ExecuteRequestAsync(new GetSticker(StickerId));
        if (result.IsSuccess)
        {
            // TODO: Navigate to StickerDetailPage (migrated in a later phase).
            await App.DisplayAlertAsync("안내", "스티커 상세 페이지는 아직 지원되지 않습니다.", Constants.PromptOk);
        }
    }
}
