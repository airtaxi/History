using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.Message;

public class DeleteMessage : IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/message/{messageId}";
    public Method Method => Method.Delete;
    public Dictionary<string, string> UrlParameters { get; set; } = [];

    public DeleteMessage(string messageId)
    {
        UrlParameters["messageId"] = messageId;
    }
}