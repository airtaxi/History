using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.Message;

public class CheckMessagePermission : IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/message/conversations/{otherUserId}/check";
    public Method Method => Method.Get;
    public Dictionary<string, string> UrlParameters { get; set; } = [];

    public CheckMessagePermission(string otherUserId)
    {
        UrlParameters["otherUserId"] = otherUserId;
    }
}