using History.Commons.DataTypes.RequestDtos;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Interfaces;
using System.Text.Json.Serialization.Metadata;
using History.Commons.Serialization;

namespace History.Commons.Api.InviteCode;

public class RejectInviteCodeRequest : IBaseRequest<InviteCodeRequestResponseDto>, IAuthRequiredRequest, IRequestWithBody
{
    public string Path { get; }
    public HttpRequestMethod Method => HttpRequestMethod.Post;
    public object Body { get; set; }
    public JsonTypeInfo BodyTypeInfo => ApiWebJsonSerializerContext.Default.GetTypeInfo(typeof(ProcessInviteCodeRequestDto));

    public RejectInviteCodeRequest(string requestId, string message = null)
    {
        Path = $"/api/invitecode/requests/{requestId}/reject";
        Body = new ProcessInviteCodeRequestDto { Message = message };
    }
}