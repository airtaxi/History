using History.Commons.DataTypes.RequestDtos;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.InviteCode;

public class RejectInviteCodeRequest : IBaseRequest<InviteCodeRequestResponseDto>, IAuthRequiredRequest, IRequestWithBody
{
    public string Path { get; }
    public Method Method => Method.Post;
    public object Body { get; set; }

    public RejectInviteCodeRequest(string requestId, string message = null)
    {
        Path = $"/api/invitecode/requests/{requestId}/reject";
        Body = new ProcessInviteCodeRequestDto { Message = message };
    }
}