using History.Commons.DataTypes.RequestDtos;
using History.Commons.Enums;
using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.Post;

public class BulkChangeDiscoveryOptionByPostIds : IAuthRequiredRequest, IRequestWithBody
{
    public string Path => "/api/post/bulk/discovery-option/by-ids";
    public Method Method => Method.Put;
    public object Body { get; set; }

    public BulkChangeDiscoveryOptionByPostIds(List<string> postIds, DiscoveryOption to) => Body = new BulkChangeDiscoveryOptionByPostIdsRequestDto
    {
        PostIds = postIds,
        To = to
    };
}
