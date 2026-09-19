using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Interfaces;

namespace History.Commons.Api.Message;

public class GetSentMessages : IBaseRequest<List<MessageResponseDto>>, IAuthRequiredRequest, IRequestWithQueryParameters
{
    public string Path => "/api/message/sent";
    public HttpRequestMethod Method => HttpRequestMethod.Get;
    public Dictionary<string, string> QueryParameters { get; set; } = [];

    public GetSentMessages(string from = null, int limit = 50)
    {
        if (!string.IsNullOrEmpty(from)) QueryParameters["from"] = from;
        QueryParameters["limit"] = limit.ToString();
    }
}