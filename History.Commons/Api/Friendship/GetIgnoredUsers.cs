using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Interfaces;

namespace History.Commons.Api.Friendship;

public class GetIgnoredUsers : IBaseRequest<List<UserResponseDto>>, IAuthRequiredRequest
{
    public string Path => "/api/friendship/ignored";
    public HttpRequestMethod Method => HttpRequestMethod.Get;
}
