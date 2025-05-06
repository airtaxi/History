using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.User;

public class ApproveUnauthorizedUser : IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/user/approve/{userId}";
    public Method Method => Method.Post;
    public Dictionary<string, string> UrlParameters { get; } = [];

    public ApproveUnauthorizedUser(string userId) => UrlParameters["userId"] = userId;
}
