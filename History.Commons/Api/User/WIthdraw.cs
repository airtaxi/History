using History.Commons.Interfaces;

namespace History.Commons.Api.User;

public class Withdraw : IAuthRequiredRequest
{
    public string Path => "/api/user/withdraw";
    public HttpRequestMethod Method => HttpRequestMethod.Post;
}
