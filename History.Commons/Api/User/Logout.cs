using History.Commons.DataTypes.RequestDtos;
using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.User;

public class Logout : IAuthRequiredRequest, IRequestWithBody
{
    public string Path => "/api/user/logout";
    public Method Method => Method.Post;
    public object Body { get; set; }

    public Logout(string refreshToken) => Body = new LogoutRequestDto
    {
        RefreshToken = refreshToken
    };
}
