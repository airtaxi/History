using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.Sticker;

public class GetSticker(string stickerId) : IBaseRequest<StickerResponseDto>, IAuthRequiredRequest
{
    public string Path => $"/api/sticker/{stickerId}";
    public Method Method => Method.Get;
}
