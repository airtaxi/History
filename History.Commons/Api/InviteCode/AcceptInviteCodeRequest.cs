using History.Commons.DataTypes.RequestDtos;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.InviteCode;

public class AcceptInviteCodeRequest : IBaseRequest<InviteCodeRequestResponseDto>, IAuthRequiredRequest, IRequestWithBody
{
    public string Path { get; }
    public Method Method => Method.Post;
    public object Body { get; set; }

    public AcceptInviteCodeRequest(string requestId, string message = null)
    {
        Path = $"/api/invitecode/requests/{requestId}/accept";
        Body = new ProcessInviteCodeRequestDto { Message = message };
    }
}