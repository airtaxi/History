using History.Commons.Interfaces;

namespace History.Commons.Api.User;

public class UnapproveUnauthorizedUser : IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/user/unapprove/{userId}";
    public HttpRequestMethod Method => HttpRequestMethod.Post;
    public Dictionary<string, string> UrlParameters { get; } = [];

    public UnapproveUnauthorizedUser(string userId) => UrlParameters["userId"] = userId;
}
