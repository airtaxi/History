using History.Commons.DataTypes.RequestDtos;
using History.Commons.Interfaces;
using System.Text.Json.Serialization.Metadata;
using History.Commons.Serialization;

namespace History.Commons.Api.User;

public class UpdateHandle : IAuthRequiredRequest, IRequestWithBody
{
    public string Path => "/api/user/handle";
    public HttpRequestMethod Method => HttpRequestMethod.Put;
    public object Body { get; set; }
    public JsonTypeInfo BodyTypeInfo => ApiWebJsonSerializerContext.Default.GetTypeInfo(typeof(UpdateUserHandleRequestDto));

    public UpdateHandle(string handle) => Body = new UpdateUserHandleRequestDto
    {
        Handle = handle
    };
}
