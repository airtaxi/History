using History.Commons.Interfaces;

namespace History.Commons.Api.User;

public class UpdateAllowSearch : IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/user/allowSearch/{allowSearch}";
    public HttpRequestMethod Method => HttpRequestMethod.Put;
    public Dictionary<string, string> UrlParameters { get; } = [];

    public UpdateAllowSearch(bool allowSearch) => UrlParameters["allowSearch"] = allowSearch.ToString().ToLower();
}
