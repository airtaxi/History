using History.Commons.DataTypes.ResponseDtos;

namespace History.MobileClient.ViewModels;

public class StickerAssetViewModel(StickerAssetResponseDto asset)
{
    public string Id => asset.Id;
    public string StickerId => asset.StickerId;
    public string MediaId => asset.MediaId;
    public string MediaUri => Utils.GenerateMediaUri(asset.MediaId);
    public bool IsAnimated => asset.IsAnimated;
}
