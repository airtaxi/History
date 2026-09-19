using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Interfaces;

namespace History.Commons.Api.Sticker;

public class SearchStickers : IBaseRequest<List<StickerResponseDto>>, IAuthRequiredRequest, IRequestWithQueryParameters
{
    public string Path => "/api/sticker/search";
    public HttpRequestMethod Method => HttpRequestMethod.Get;
    public Dictionary<string, string> QueryParameters { get; set; } = [];

    public SearchStickers(string query, string from = null, int limit = 20)
    {
        QueryParameters["query"] = query;
        QueryParameters["limit"] = limit.ToString();
        if (from != null) QueryParameters["from"] = from;
    }
}
