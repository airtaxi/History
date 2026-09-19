using History.Commons.Enums;
using History.Commons.Interfaces;

namespace History.Commons.Api.Post;

public class BulkDeletePosts : IAuthRequiredRequest, IRequestWithQueryParameters
{
    public string Path => "/api/post/bulk";
    public HttpRequestMethod Method => HttpRequestMethod.Delete;

    public Dictionary<string, string> QueryParameters { get; } = [];

    public BulkDeletePosts(DiscoveryOption? discoveryOption = null)
    {
        if (discoveryOption.HasValue) QueryParameters["discoveryOption"] = discoveryOption.Value.ToString();
    }
}
