using History.Commons.DataTypes.RequestDtos;
using History.Commons.Interfaces;
using System.Text.Json.Serialization.Metadata;
using History.Commons.Serialization;

namespace History.Commons.Api.Post;

public class BulkDeletePostsByPostIds : IAuthRequiredRequest, IRequestWithBody
{
    public string Path => "/api/post/bulk/by-ids";
    public HttpRequestMethod Method => HttpRequestMethod.Delete;
    public object Body { get; set; }
    public JsonTypeInfo BodyTypeInfo => ApiWebJsonSerializerContext.Default.GetTypeInfo(typeof(BulkDeletePostsByPostIdsRequestDto));

    public BulkDeletePostsByPostIds(List<string> postIds) => Body = new BulkDeletePostsByPostIdsRequestDto { PostIds = postIds };
}
