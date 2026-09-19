using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Interfaces;

namespace History.Commons.Api.User;

public class GetUnauthorizedUsers : IBaseRequest<List<UserResponseDto>>, IAuthRequiredRequest
{
    public string Path => "/api/user/unauthorized-users";
    public HttpRequestMethod Method => HttpRequestMethod.Get;
}
