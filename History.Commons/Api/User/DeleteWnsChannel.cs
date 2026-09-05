using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.PushNotification;

public class DeleteWnsChannel : IAuthRequiredRequest, IRequestWithQueryParameters
{
    public string Path => "/api/user/wns-channels";
    public Method Method => Method.Delete;
    public Dictionary<string, string> QueryParameters { get; set; } = [];

    public DeleteWnsChannel(string channelUri) => QueryParameters["channelUri"] = channelUri;
}