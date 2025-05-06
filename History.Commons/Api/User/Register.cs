using History.Commons.DataTypes.RequestDtos;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.User;

public class Register : IBaseRequest<OAuthLoginResponseDto>, IRequestWithBody
{
    public string Path => "/api/user/register";
    public Method Method => Method.Post;
    public object Body { get; set; }

    public Register(string idToken, SocialService provider) => Body = new OAuthLoginRequestDto
    {
        IdToken = idToken,
        Provider = provider
    };
}
