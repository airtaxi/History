using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Interfaces;

namespace History.Commons.Api.Friendship;

public class GetBlockedUsers : IBaseRequest<List<UserResponseDto>>, IAuthRequiredRequest
{
    public string Path => "/api/friendship/blocked";
    public HttpRequestMethod Method => HttpRequestMethod.Get;
}
