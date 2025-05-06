using Google.Apis.Auth.OAuth2.Requests;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.User;

public class RefreshToken : IBaseRequest<OAuthLoginResponseDto>, IRequestWithBody
{
    public string Path => "/api/user/refresh-token";
    public Method Method => Method.Post;
    public object Body { get; set; }

    public RefreshToken(string refreshToken) => Body = new RefreshTokenRequest
    {
        RefreshToken = refreshToken
    };
}
