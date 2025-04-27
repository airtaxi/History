using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.User;

public class FindUsersByNickname : IOptionalAuthRequest, IRequestWithQueryParameters
{
    public string Path => "/api/user/nickname-search/{query}";
    public Method Method => Method.Get;
    public Dictionary<string, string> QueryParameters { get; } = new();

    public FindUsersByNickname(string query) => QueryParameters["query"] = query;
}
