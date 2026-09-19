using History.Commons.DataTypes.RequestDtos;
using History.Commons.Interfaces;
using System.Text.Json.Serialization.Metadata;
using History.Commons.Serialization;

namespace History.Commons.Api.InviteCode;

public class RequestInviteCodes : IBaseRequest<string>, IAuthRequiredRequest, IRequestWithBody
{
    public string Path => "/api/invitecode/request";
    public HttpRequestMethod Method => HttpRequestMethod.Post;
    public object Body { get; set; }
    public JsonTypeInfo BodyTypeInfo => ApiWebJsonSerializerContext.Default.GetTypeInfo(typeof(CreateInviteCodeRequestDto));

    public RequestInviteCodes(string reason, int count) => Body = new CreateInviteCodeRequestDto
    {
        Reason = reason,
        Count = count
    };
}