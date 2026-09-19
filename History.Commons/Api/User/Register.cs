using History.Commons.DataTypes.RequestDtos;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using History.Commons.Interfaces;
using System.Text.Json.Serialization.Metadata;
using History.Commons.Serialization;

namespace History.Commons.Api.User;

public class Register : IBaseRequest<OAuthLoginResponseDto>, IRequestWithBody
{
    public string Path => "/api/user/register";
    public HttpRequestMethod Method => HttpRequestMethod.Post;
    public object Body { get; set; }
    public JsonTypeInfo BodyTypeInfo => ApiWebJsonSerializerContext.Default.GetTypeInfo(typeof(OAuthRegisterRequestDto));

    public Register(string idToken, SocialService provider, string name, string inviteCode = null) => Body = new OAuthRegisterRequestDto
    {
        IdToken = idToken,
        Provider = provider,
        Name = name,
        InviteCode = inviteCode
    };
}
