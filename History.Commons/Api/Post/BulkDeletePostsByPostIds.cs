using History.Commons.DataTypes.RequestDtos;
using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.Post;

public class BulkDeletePostsByPostIds : IAuthRequiredRequest, IRequestWithBody
{
    public string Path => "/api/post/bulk/by-ids";
    public Method Method => Method.Delete;
    public object Body { get; set; }

    public BulkDeletePostsByPostIds(List<string> postIds) => Body = new BulkDeletePostsByPostIdsRequestDto { PostIds = postIds };
}
