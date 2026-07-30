using History.Commons.DataTypes.RequestDtos;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.InviteCode;

public class CreateInviteCodeByAdmin : IBaseRequest<List<InviteCodeResponseDto>>, IAuthRequiredRequest, IRequestWithBody
{
    public string Path => "/api/invitecode";
    public Method Method => Method.Post;
    public object Body { get; set; }

    public CreateInviteCodeByAdmin(string ownerId, int count) => Body = new CreateInviteCodeByAdminRequestDto
    {
        OwnerId = ownerId,
        Count = count
    };
}