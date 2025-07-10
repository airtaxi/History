using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.Message;

public class GetMessage : IBaseRequest<MessageResponseDto>, IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/message/{messageId}";
    public Method Method => Method.Get;
    public Dictionary<string, string> UrlParameters { get; set; } = [];

    public GetMessage(string messageId) => UrlParameters["messageId"] = messageId;
}