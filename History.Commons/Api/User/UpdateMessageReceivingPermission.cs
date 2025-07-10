using History.Commons.DataTypes.RequestDtos;
using History.Commons.Enums;
using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.User;

public class UpdateMessageReceivingPermission : IAuthRequiredRequest, IRequestWithBody
{
    public string Path => "/api/user/message-receiving-permission";
    public Method Method => Method.Put;
    public object Body { get; set; }

    public UpdateMessageReceivingPermission(AccessPermission permission)
    {
        Body = new UpdateMessageReceivingPermissionRequestDto { Permission = permission };
    }
}