using History.Commons.DataTypes.RequestDtos;
using History.Commons.Interfaces;
using System.Text.Json.Serialization.Metadata;
using History.Commons.Serialization;

namespace History.Commons.Api.User;

public class Logout : IAuthRequiredRequest, IRequestWithBody
{
    public string Path => "/api/user/logout";
    public HttpRequestMethod Method => HttpRequestMethod.Post;
    public object Body { get; set; }
    public JsonTypeInfo BodyTypeInfo => ApiWebJsonSerializerContext.Default.GetTypeInfo(typeof(LogoutRequestDto));

    public Logout(string refreshToken) => Body = new LogoutRequestDto
    {
        RefreshToken = refreshToken
    };
}
