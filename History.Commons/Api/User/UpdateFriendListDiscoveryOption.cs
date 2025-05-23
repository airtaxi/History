using History.Commons.Enums;
using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.User;

public class UpdateFriendListDiscoveryOption : IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/user/friend-list-discovery-option/{discoveryOption}";
    public Method Method => Method.Put;
    public Dictionary<string, string> UrlParameters { get; } = [];

    public UpdateFriendListDiscoveryOption(DiscoveryOption discoveryOption) => UrlParameters["discoveryOption"] = discoveryOption.ToString();
}
