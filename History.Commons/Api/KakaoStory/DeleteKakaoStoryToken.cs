using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.KakaoStory;

public class DeleteKakaoStoryToken : IAuthRequiredRequest
{
    public string Path => "/api/kakaostory/token";
    public Method Method => Method.Delete;
}
