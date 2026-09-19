using History.Commons.DataTypes.RequestDtos;
using History.Commons.Enums;
using History.Commons.Interfaces;
using System.Text.Json.Serialization.Metadata;
using History.Commons.Serialization;

namespace History.Commons.Api.User;

public class UpdateMessageReceivingPermission : IAuthRequiredRequest, IRequestWithBody
{
    public string Path => "/api/user/message-receiving-permission";
    public HttpRequestMethod Method => HttpRequestMethod.Put;
    public object Body { get; set; }
    public JsonTypeInfo BodyTypeInfo => ApiWebJsonSerializerContext.Default.GetTypeInfo(typeof(UpdateMessageReceivingPermissionRequestDto));

    public UpdateMessageReceivingPermission(AccessPermission permission)
    {
        Body = new UpdateMessageReceivingPermissionRequestDto { Permission = permission };
    }
}