using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.Message;

// 받은 쪽지 목록 조회용
public class GetReceivedMessages : IBaseRequest<List<MessageResponseDto>>, IAuthRequiredRequest, IRequestWithQueryParameters
{
    public string Path => "/api/message/received";
    public Method Method => Method.Get;
    public Dictionary<string, string> QueryParameters { get; set; } = [];

    public GetReceivedMessages(string from = null, int limit = 50)
    {
        if (!string.IsNullOrEmpty(from))
            QueryParameters["from"] = from;
        QueryParameters["limit"] = limit.ToString();
    }
}
