using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.Message;

public class CheckMessagePermission : IAuthRequiredRequest, IRequestWithQueryParameters
{
    public string Path => "/api/message/check-permission";
    public Method Method => Method.Get;
    public Dictionary<string, string> QueryParameters { get; set; } = [];

    public CheckMessagePermission(string receiverId)
    {
        QueryParameters["receiverId"] = receiverId;
    }
}