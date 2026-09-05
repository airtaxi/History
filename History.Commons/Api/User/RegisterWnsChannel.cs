using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.PushNotification;

public class RegisterWnsChannel : IAuthRequiredRequest, IRequestWithQueryParameters
{
    public string Path => "/api/user/wns-channels";
    public Method Method => Method.Put;
    public Dictionary<string, string> QueryParameters { get; set; } = [];

    public RegisterWnsChannel(string channelUri) => QueryParameters["channelUri"] = channelUri;
}