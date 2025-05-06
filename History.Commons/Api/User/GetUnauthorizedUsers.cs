using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.User;

public class GetUnauthorizedUsers : IBaseRequest<List<UserResponseDto>>, IAuthRequiredRequest
{
    public string Path => "/api/user/unauthorized-users";
    public Method Method => Method.Get;
}
