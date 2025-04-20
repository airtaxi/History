using Google.Apis.Auth.OAuth2.Requests;
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

public class RefreshToken : IBaseRequest<OAuthLoginResponseDto>, IRequestWithBody
{
    public string Path => "/api/user/login";
    public Method Method => Method.Post;
    public object Body { get; set; }

    public RefreshToken(string refreshToken) => Body = new RefreshTokenRequest
    {
        RefreshToken = refreshToken
    };
}
