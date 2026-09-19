using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Interfaces;

namespace History.Commons.Api.User;

public class GetMyProfile : IBaseRequest<UserResponseDto>, IAuthRequiredRequest
{
    public string Path => "/api/user/me";
    public HttpRequestMethod Method => HttpRequestMethod.Get;
}
