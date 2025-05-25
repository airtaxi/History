using History.Commons.Enums;
using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.User;

public class UpdatePinnedPost : IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/user/pinned-post/{pinnedPostId}";
    public Method Method => Method.Put;
    public Dictionary<string, string> UrlParameters { get; } = [];

    public UpdatePinnedPost(string pinnedPostId) => UrlParameters["pinnedPostId"] = pinnedPostId;
}
