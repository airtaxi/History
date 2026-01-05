using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.Sticker;

/// <summary>
/// 스티커 에셋 사용을 기록합니다.
/// </summary>
public class RecordStickerUsage : IBaseRequest<string>, IAuthRequiredRequest
{
    public string Path { get; }
    public Method Method => Method.Post;

    public RecordStickerUsage(string stickerId, string assetId)
    {
        Path = $"/api/sticker/{stickerId}/assets/{assetId}/use";
    }
}
