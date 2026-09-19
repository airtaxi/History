using History.Commons.Interfaces;

namespace History.Commons.Api.User;

public class DeleteProfileMedia : IAuthRequiredRequest
{
    public string Path => "/api/user/profile-media";
    public HttpRequestMethod Method => HttpRequestMethod.Delete;
}
