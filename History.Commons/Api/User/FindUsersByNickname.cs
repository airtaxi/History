using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.User;

public class FindUsersByNickname : IBaseRequest<List<UserResponseDto>>, IOptionalAuthRequest, IRequestWithQueryParameters
{
    public string Path => "/api/user/nickname-search/{query}";
    public Method Method => Method.Get;
    public Dictionary<string, string> QueryParameters { get; } = new();

    public FindUsersByNickname(string query) => QueryParameters["query"] = query;
}
