using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Interfaces;

namespace History.Commons.Api.User;

public class GetUser : IBaseRequest<UserResponseDto>, IOptionalAuthRequest, IRequestWithUrlParameters
{
    public string Path => "/api/user/{userId}";
    public HttpRequestMethod Method => HttpRequestMethod.Get;
    public Dictionary<string, string> UrlParameters { get; set; } = [];

    public GetUser(string userId) => UrlParameters["userId"] = userId;
}
