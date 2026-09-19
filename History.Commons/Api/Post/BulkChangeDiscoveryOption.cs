using History.Commons.DataTypes.RequestDtos;
using History.Commons.Enums;
using History.Commons.Interfaces;
using System.Text.Json.Serialization.Metadata;
using History.Commons.Serialization;

namespace History.Commons.Api.Post;

public class BulkChangeDiscoveryOption : IAuthRequiredRequest, IRequestWithBody
{
    public string Path => "/api/post/bulk/discovery-option";
    public HttpRequestMethod Method => HttpRequestMethod.Put;
    public object Body { get; set; }
    public JsonTypeInfo BodyTypeInfo => ApiWebJsonSerializerContext.Default.GetTypeInfo(typeof(BulkChangeDiscoveryOptionRequestDto));

    public BulkChangeDiscoveryOption(DiscoveryOption? from, DiscoveryOption to)
    {
        Body = new BulkChangeDiscoveryOptionRequestDto
        {
            From = from,
            To = to
        };
    }
}
