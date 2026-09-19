using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Interfaces;

namespace History.Commons.Api.Message;

public class GetReceivedMessages : IBaseRequest<List<MessageResponseDto>>, IAuthRequiredRequest, IRequestWithQueryParameters
{
    public string Path => "/api/message/received";
    public HttpRequestMethod Method => HttpRequestMethod.Get;
    public Dictionary<string, string> QueryParameters { get; set; } = [];

    public GetReceivedMessages(string from = null, int limit = 50)
    {
        if (!string.IsNullOrEmpty(from)) QueryParameters["from"] = from;
        QueryParameters["limit"] = limit.ToString();
    }
}
