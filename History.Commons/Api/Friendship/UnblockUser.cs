using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.Friendship;

public class UnblockUser : IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/friendship/block/{blockedUserId}";
    public Method Method => Method.Delete;
    public Dictionary<string, string> UrlParameters { get; set; } = [];

    public UnblockUser(string blockedUserId) => UrlParameters["blockedUserId"] = blockedUserId;
}
