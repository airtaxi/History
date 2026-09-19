using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Interfaces;
using System.Text.Json.Serialization.Metadata;
using History.Commons.Serialization;
using History.Commons.DataTypes.RequestDtos;

namespace History.Commons.Api.User;

public class RefreshToken : IBaseRequest<OAuthLoginResponseDto>, IRequestWithBody
{
    public string Path => "/api/user/refresh-token";
    public HttpRequestMethod Method => HttpRequestMethod.Post;
    public object Body { get; set; }
    public JsonTypeInfo BodyTypeInfo => ApiWebJsonSerializerContext.Default.GetTypeInfo(typeof(RefreshTokenRequestDto));

    public RefreshToken(string refreshToken) => Body = new RefreshTokenRequestDto
    {
        RefreshToken = refreshToken
    };
}
