using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.Friendship;

public class BlockUser : IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/friendship/block/{userIdToBlock}";
    public Method Method => Method.Post;
    public Dictionary<string, string> UrlParameters { get; set; } = [];

    public BlockUser(string userIdToBlock) => UrlParameters["userIdToBlock"] = userIdToBlock;
}
