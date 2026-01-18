using History.Commons.Enums;
using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.Post;

public class BulkDeletePosts : IAuthRequiredRequest, IRequestWithQueryParameters
{
    public string Path => "/api/post/bulk";
    public Method Method => Method.Delete;

    public Dictionary<string, string> QueryParameters { get; } = [];

    public BulkDeletePosts(DiscoveryOption? discoveryOption = null)
    {
        if (discoveryOption.HasValue) QueryParameters["discoveryOption"] = discoveryOption.Value.ToString();
    }
}
