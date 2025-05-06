using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.Friendship;

public class SendFriendRequest : IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/friendship/request/{receiverId}";
    public Method Method => Method.Post;
    public Dictionary<string, string> UrlParameters { get; set; } = [];

    public SendFriendRequest(string receiverId) => UrlParameters["receiverId"] = receiverId;
}
