using History.Commons.Interfaces;

namespace History.Commons.Api.Friendship;

public class SendFriendRequest : IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/friendship/request/{receiverId}";
    public HttpRequestMethod Method => HttpRequestMethod.Post;
    public Dictionary<string, string> UrlParameters { get; set; } = [];

    public SendFriendRequest(string receiverId) => UrlParameters["receiverId"] = receiverId;
}
