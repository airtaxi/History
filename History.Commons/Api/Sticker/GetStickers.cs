using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Interfaces;

namespace History.Commons.Api.Sticker;

public class GetStickers : IBaseRequest<List<StickerResponseDto>>, IAuthRequiredRequest, IRequestWithQueryParameters
{
    public string Path => "/api/sticker";
    public HttpRequestMethod Method => HttpRequestMethod.Get;
    public Dictionary<string, string> QueryParameters { get; set; } = [];

    public GetStickers(string from = null, int limit = 20)
    {
        QueryParameters["limit"] = limit.ToString();
        if (from != null) QueryParameters["from"] = from;
    }
}
