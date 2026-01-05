using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.Sticker;

/// <summary>
/// 스티커 구독을 취소합니다.
/// </summary>
public class UnsubscribeSticker : IBaseRequest<string>, IAuthRequiredRequest
{
    public string Path { get; }
    public Method Method => Method.Delete;

    public UnsubscribeSticker(string stickerId)
    {
        Path = $"/api/sticker/{stickerId}/subscribe";
    }
}
