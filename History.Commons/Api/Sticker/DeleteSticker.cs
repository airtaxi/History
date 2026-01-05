using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.Sticker;

public class DeleteSticker(string stickerId) : IAuthRequiredRequest
{
    public string Path => $"/api/sticker/{stickerId}";
    public Method Method => Method.Delete;
}
