using History.Commons.Interfaces;

namespace History.Commons.Api.PushNotification;

public class DeleteWnsChannel : IAuthRequiredRequest, IRequestWithQueryParameters
{
    public string Path => "/api/user/wns-channels";
    public HttpRequestMethod Method => HttpRequestMethod.Delete;
    public Dictionary<string, string> QueryParameters { get; set; } = [];

    public DeleteWnsChannel(string channelUri) => QueryParameters["channelUri"] = channelUri;
}