using History.Commons.Interfaces;

namespace History.Commons.Api.Friendship;

public class BlockUser : IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/friendship/block/{userIdToBlock}";
    public HttpRequestMethod Method => HttpRequestMethod.Post;
    public Dictionary<string, string> UrlParameters { get; set; } = [];

    public BlockUser(string userIdToBlock) => UrlParameters["userIdToBlock"] = userIdToBlock;
}
