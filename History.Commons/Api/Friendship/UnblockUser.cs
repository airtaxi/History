using History.Commons.Interfaces;

namespace History.Commons.Api.Friendship;

public class UnblockUser : IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/friendship/block/{blockedUserId}";
    public HttpRequestMethod Method => HttpRequestMethod.Delete;
    public Dictionary<string, string> UrlParameters { get; set; } = [];

    public UnblockUser(string blockedUserId) => UrlParameters["blockedUserId"] = blockedUserId;
}
