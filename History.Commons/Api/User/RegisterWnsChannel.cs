using History.Commons.Interfaces;

namespace History.Commons.Api.PushNotification;

public class RegisterWnsChannel : IAuthRequiredRequest, IRequestWithQueryParameters
{
    public string Path => "/api/user/wns-channels";
    public HttpRequestMethod Method => HttpRequestMethod.Put;
    public Dictionary<string, string> QueryParameters { get; set; } = [];

    public RegisterWnsChannel(string channelUri) => QueryParameters["channelUri"] = channelUri;
}