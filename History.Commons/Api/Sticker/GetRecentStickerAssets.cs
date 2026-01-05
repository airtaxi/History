using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.Sticker;

/// <summary>
/// 최근 사용한 스티커 에셋을 조회합니다.
/// </summary>
public class GetRecentStickerAssets : IBaseRequest<List<StickerAssetResponseDto>>, IAuthRequiredRequest, IRequestWithQueryParameters
{
    public string Path => "/api/sticker/recent";
    public Method Method => Method.Get;
    public Dictionary<string, string> QueryParameters { get; set; } = [];

    public GetRecentStickerAssets(int limit = 20)
    {
        QueryParameters["limit"] = limit.ToString();
    }
}
