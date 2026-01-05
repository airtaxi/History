using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.Sticker;

public class GetStickerAssets(string stickerId) : IBaseRequest<List<StickerAssetResponseDto>>, IAuthRequiredRequest
{
    public string Path => $"/api/sticker/{stickerId}/assets";
    public Method Method => Method.Get;
}
