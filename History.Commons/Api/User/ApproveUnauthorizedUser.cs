using History.Commons.Interfaces;

namespace History.Commons.Api.User;

public class ApproveUnauthorizedUser : IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/user/approve/{userId}";
    public HttpRequestMethod Method => HttpRequestMethod.Post;
    public Dictionary<string, string> UrlParameters { get; } = [];

    public ApproveUnauthorizedUser(string userId) => UrlParameters["userId"] = userId;
}
