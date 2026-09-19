using History.Commons.Interfaces;

namespace History.Commons.Api.User;

public class DeleteBackgroundMedia : IAuthRequiredRequest
{
    public string Path => "/api/user/background-media";
    public HttpRequestMethod Method => HttpRequestMethod.Delete;
}
