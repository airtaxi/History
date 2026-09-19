using History.Commons.DataTypes.RequestDtos;
using History.Commons.Enums;
using History.Commons.Interfaces;
using System.Text.Json.Serialization.Metadata;
using History.Commons.Serialization;

namespace History.Commons.Api.Post;

public class BulkChangeDiscoveryOptionByPostIds : IAuthRequiredRequest, IRequestWithBody
{
    public string Path => "/api/post/bulk/discovery-option/by-ids";
    public HttpRequestMethod Method => HttpRequestMethod.Put;
    public object Body { get; set; }
    public JsonTypeInfo BodyTypeInfo => ApiWebJsonSerializerContext.Default.GetTypeInfo(typeof(BulkChangeDiscoveryOptionByPostIdsRequestDto));

    public BulkChangeDiscoveryOptionByPostIds(List<string> postIds, DiscoveryOption to) => Body = new BulkChangeDiscoveryOptionByPostIdsRequestDto
    {
        PostIds = postIds,
        To = to
    };
}
