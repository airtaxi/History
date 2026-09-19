using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Interfaces;

namespace History.Commons.Api.Sticker;

public class GetStickersByUserId : IBaseRequest<List<StickerResponseDto>>, IAuthRequiredRequest, IRequestWithQueryParameters
{
    public string Path { get; }
    public HttpRequestMethod Method => HttpRequestMethod.Get;
    public Dictionary<string, string> QueryParameters { get; set; } = [];

    public GetStickersByUserId(string userId, string from = null, int limit = 20)
    {
        Path = $"/api/sticker/user/{userId}";
        QueryParameters["limit"] = limit.ToString();
        if (from != null) QueryParameters["from"] = from;
    }
}
