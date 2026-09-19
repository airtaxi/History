using History.Commons.Interfaces;

namespace History.Commons.Api.Message;

public class CheckMessagePermission : IAuthRequiredRequest, IRequestWithQueryParameters
{
    public string Path => "/api/message/check-permission";
    public HttpRequestMethod Method => HttpRequestMethod.Post;
    public Dictionary<string, string> QueryParameters { get; set; } = [];

    public CheckMessagePermission(string receiverId) => QueryParameters["receiverId"] = receiverId;
}