using History.Commons.DataTypes.RequestDtos;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using History.Commons.Interfaces;
using System.Text.Json.Serialization.Metadata;
using History.Commons.Serialization;

namespace History.Commons.Api.User;

public class Login : IBaseRequest<OAuthLoginResponseDto>, IRequestWithBody
{
    public string Path => "/api/user/login";
    public HttpRequestMethod Method => HttpRequestMethod.Post;
    public object Body { get; set; }
    public JsonTypeInfo BodyTypeInfo => ApiWebJsonSerializerContext.Default.GetTypeInfo(typeof(OAuthLoginRequestDto));

    public Login(string idToken, SocialService provider) => Body = new OAuthLoginRequestDto
    {
        IdToken = idToken,
        Provider = provider
    };
}
