using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.Message;

public class MarkMessageAsRead : IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/message/{messageId}/read";
    public Method Method => Method.Post;
    public Dictionary<string, string> UrlParameters { get; set; } = [];

    public MarkMessageAsRead(string messageId) => UrlParameters["messageId"] = messageId;
}