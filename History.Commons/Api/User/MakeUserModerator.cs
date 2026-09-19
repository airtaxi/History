using History.Commons.Interfaces;

namespace History.Commons.Api.User;

public class MakeUserModerator : IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/user/make-moderator/{userId}";
    public HttpRequestMethod Method => HttpRequestMethod.Post;
    public Dictionary<string, string> UrlParameters { get; } = [];

    public MakeUserModerator(string userId) => UrlParameters["userId"] = userId;
}
