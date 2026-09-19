using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Interfaces;

namespace History.Commons.Api.Friendship;

public class GetWaitingRequests : IBaseRequest<List<UserResponseDto>>, IAuthRequiredRequest
{
    public string Path => "/api/friendship/waiting";
    public HttpRequestMethod Method => HttpRequestMethod.Get;
}
