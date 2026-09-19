using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Interfaces;

namespace History.Commons.Api.Sticker;

public class GetSticker(string stickerId) : IBaseRequest<StickerResponseDto>, IAuthRequiredRequest
{
    public string Path => $"/api/sticker/{stickerId}";
    public HttpRequestMethod Method => HttpRequestMethod.Get;
}
