using History.Commons.Enums;
using History.Commons.Interfaces;

namespace History.Commons.Api.User;

public class UpdateFriendListDiscoveryOption : IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/user/friend-list-discovery-option/{discoveryOption}";
    public HttpRequestMethod Method => HttpRequestMethod.Put;
    public Dictionary<string, string> UrlParameters { get; } = [];

    public UpdateFriendListDiscoveryOption(DiscoveryOption discoveryOption) => UrlParameters["discoveryOption"] = discoveryOption.ToString();
}
