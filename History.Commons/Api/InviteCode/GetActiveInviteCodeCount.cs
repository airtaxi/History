using History.Commons.Interfaces;

namespace History.Commons.Api.InviteCode;

public class GetActiveInviteCodeCount : IBaseRequest<int>, IAuthRequiredRequest, IRequestWithQueryParameters
{
    public string Path => "/api/invitecode/active-count";
    public HttpRequestMethod Method => HttpRequestMethod.Get;
    public Dictionary<string, string> QueryParameters { get; set; } = [];

    public GetActiveInviteCodeCount(string userId = null)
    {
        if (userId != null) QueryParameters["userId"] = userId;
    }
}