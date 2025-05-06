using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.User;

public class GetUser : IBaseRequest<UserResponseDto>, IOptionalAuthRequest, IRequestWithUrlParameters
{
    public string Path => "/api/user/{userId}";
    public Method Method => Method.Get;
    public Dictionary<string, string> UrlParameters { get; set; } = [];

    public GetUser(string userId) => UrlParameters["userId"] = userId;
}
