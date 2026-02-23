using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.User;

public class ReadNotificationsByMessageId : IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/user/notifications/read-by-message/{messageId}";
    public Method Method => Method.Post;
    public Dictionary<string, string> UrlParameters { get; set; } = [];

    public ReadNotificationsByMessageId(string messageId) => UrlParameters["messageId"] = messageId;
}
