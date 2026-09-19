using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Interfaces;

namespace History.Commons.Api.InviteCode;

public class GetInviteCodeRequests : IBaseRequest<List<InviteCodeRequestResponseDto>>, IAuthRequiredRequest, IRequestWithQueryParameters
{
    public string Path => "/api/invitecode/requests";
    public HttpRequestMethod Method => HttpRequestMethod.Get;
    public Dictionary<string, string> QueryParameters { get; set; } = [];

    public GetInviteCodeRequests(string from = null, int limit = 20)
    {
        QueryParameters["limit"] = limit.ToString();
        if (from != null) QueryParameters["from"] = from;
    }
}