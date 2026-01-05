using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.Sticker;

/// <summary>
/// 스티커를 구독합니다.
/// </summary>
public class SubscribeSticker : IBaseRequest<string>, IAuthRequiredRequest
{
    public string Path { get; }
    public Method Method => Method.Post;

    public SubscribeSticker(string stickerId)
    {
        Path = $"/api/sticker/{stickerId}/subscribe";
    }
}
