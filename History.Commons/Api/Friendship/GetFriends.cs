using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.Friendship;

public class GetFriends : IBaseRequest<List<UserResponseDto>>, IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/friendship/{userId}";
    public Method Method => Method.Get;
    public Dictionary<string, string> UrlParameters { get; set; } = [];

    public GetFriends(string userId) => UrlParameters["userId"] = userId;
}
