using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.Friendship;

public class AcceptFriendRequest : IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/friendship/request/{userIdToAccept}/accept";
    public Method Method => Method.Post;
    public Dictionary<string, string> UrlParameters { get; set; } = [];

    public AcceptFriendRequest(string userIdToAccept) => UrlParameters["userIdToAccept"] = userIdToAccept;
}
