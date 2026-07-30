using History.Commons.DataTypes.RequestDtos;
using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.InviteCode;

public class RequestInviteCodes : IBaseRequest<string>, IAuthRequiredRequest, IRequestWithBody
{
    public string Path => "/api/invitecode/request";
    public Method Method => Method.Post;
    public object Body { get; set; }

    public RequestInviteCodes(string reason, int count) => Body = new CreateInviteCodeRequestDto
    {
        Reason = reason,
        Count = count
    };
}