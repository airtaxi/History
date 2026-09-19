using History.Commons.DataTypes.RequestDtos;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Interfaces;
using System.Text.Json.Serialization.Metadata;
using History.Commons.Serialization;

namespace History.Commons.Api.InviteCode;

public class CreateInviteCodeByAdmin : IBaseRequest<List<InviteCodeResponseDto>>, IAuthRequiredRequest, IRequestWithBody
{
    public string Path => "/api/invitecode";
    public HttpRequestMethod Method => HttpRequestMethod.Post;
    public object Body { get; set; }
    public JsonTypeInfo BodyTypeInfo => ApiWebJsonSerializerContext.Default.GetTypeInfo(typeof(CreateInviteCodeByAdminRequestDto));

    public CreateInviteCodeByAdmin(string ownerId, int count) => Body = new CreateInviteCodeByAdminRequestDto
    {
        OwnerId = ownerId,
        Count = count
    };
}