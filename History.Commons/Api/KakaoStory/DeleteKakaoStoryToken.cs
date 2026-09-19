using History.Commons.Interfaces;

namespace History.Commons.Api.KakaoStory;

public class DeleteKakaoStoryToken : IAuthRequiredRequest
{
    public string Path => "/api/kakaostory/token";
    public HttpRequestMethod Method => HttpRequestMethod.Delete;
}
