using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.Friendship;

public class DeclineFriendRequest : IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/friendship/request/{userIdToDecline}/decline";
    public Method Method => Method.Post;
    public Dictionary<string, string> UrlParameters { get; set; } = [];

    public DeclineFriendRequest(string userIdToDecline) => UrlParameters["userIdToDecline"] = userIdToDecline;
}
