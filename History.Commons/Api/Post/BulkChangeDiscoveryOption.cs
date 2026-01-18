using History.Commons.DataTypes.RequestDtos;
using History.Commons.Enums;
using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.Post;

public class BulkChangeDiscoveryOption : IAuthRequiredRequest, IRequestWithBody
{
    public string Path => "/api/post/bulk/discovery-option";
    public Method Method => Method.Put;
    public object Body { get; set; }

    public BulkChangeDiscoveryOption(DiscoveryOption? from, DiscoveryOption to)
    {
        Body = new BulkChangeDiscoveryOptionRequestDto
        {
            From = from,
            To = to
        };
    }
}
