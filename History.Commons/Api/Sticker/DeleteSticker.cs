using History.Commons.Interfaces;

namespace History.Commons.Api.Sticker;

public class DeleteSticker(string stickerId) : IAuthRequiredRequest
{
    public string Path => $"/api/sticker/{stickerId}";
    public HttpRequestMethod Method => HttpRequestMethod.Delete;
}
