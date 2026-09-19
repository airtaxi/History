using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Interfaces;

namespace History.Commons.Api.InviteCode;

public class GetMyInviteCodes : IBaseRequest<List<InviteCodeResponseDto>>, IAuthRequiredRequest, IRequestWithQueryParameters
{
    public string Path => "/api/invitecode";
    public HttpRequestMethod Method => HttpRequestMethod.Get;
    public Dictionary<string, string> QueryParameters { get; set; } = [];

    public GetMyInviteCodes(string from = null, int limit = 20)
    {
        QueryParameters["limit"] = limit.ToString();
        if (from != null) QueryParameters["from"] = from;
    }
}