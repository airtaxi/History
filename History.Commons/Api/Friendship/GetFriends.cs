using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Interfaces;

namespace History.Commons.Api.Friendship;

public class GetFriends : IBaseRequest<List<UserResponseDto>>, IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/friendship/{userId}";
    public HttpRequestMethod Method => HttpRequestMethod.Get;
    public Dictionary<string, string> UrlParameters { get; set; } = [];

    public GetFriends(string userId) => UrlParameters["userId"] = userId;
}
