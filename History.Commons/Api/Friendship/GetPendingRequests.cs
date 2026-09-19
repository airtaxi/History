using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Interfaces;

namespace History.Commons.Api.Friendship;

public class GetPendingRequests : IBaseRequest<List<UserResponseDto>>, IAuthRequiredRequest
{
    public string Path => "/api/friendship/pending";
    public HttpRequestMethod Method => HttpRequestMethod.Get;
}
