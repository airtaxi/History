using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.Message;

public class GetMessages : IBaseRequest<List<MessageResponseDto>>, IAuthRequiredRequest, IRequestWithUrlParameters, IRequestWithQueryParameters
{
    public string Path => "/api/message/conversations/{conversationId}/messages";
    public Method Method => Method.Get;
    public Dictionary<string, string> UrlParameters { get; set; } = [];
    public Dictionary<string, string> QueryParameters { get; set; } = [];

    public GetMessages(string conversationId, string from = null, int limit = 50)
    {
        UrlParameters["conversationId"] = conversationId;
        if (!string.IsNullOrEmpty(from))
            QueryParameters["from"] = from;
        QueryParameters["limit"] = limit.ToString();
    }
}