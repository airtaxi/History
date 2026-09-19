using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Interfaces;

namespace History.Commons.Api.User;

public class GetModerators : IBaseRequest<List<UserResponseDto>>, IAuthRequiredRequest
{
    public string Path => "/api/user/moderators";
    public HttpRequestMethod Method => HttpRequestMethod.Get;
}
