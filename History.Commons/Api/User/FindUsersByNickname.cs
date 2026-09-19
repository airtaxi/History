using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Interfaces;

namespace History.Commons.Api.User;

public class FindUsersByNickname : IBaseRequest<List<UserResponseDto>>, IOptionalAuthRequest, IRequestWithUrlParameters
{
    public string Path => "/api/user/nickname-search/{query}";
    public HttpRequestMethod Method => HttpRequestMethod.Get;
    public Dictionary<string, string> UrlParameters { get; } = [];

    public FindUsersByNickname(string query) => UrlParameters["query"] = query;
}
