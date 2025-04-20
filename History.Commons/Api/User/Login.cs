using History.Commons.DataTypes.RequestDtos;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using History.Commons.Interfaces;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.Commons.Api.User;

public class Login : IBaseRequest<OAuthLoginResponseDto>, IRequestWithBody
{
    public string Path => "/api/user/login";
    public Method Method => Method.Post;
    public object Body { get; set; }

    public Login(string idToken, SocialService provider) => Body = new OAuthLoginRequestDto
    {
        IdToken = idToken,
        Provider = provider
    };
}
