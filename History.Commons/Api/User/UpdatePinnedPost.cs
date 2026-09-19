using History.Commons.Enums;
using History.Commons.Interfaces;

namespace History.Commons.Api.User;

public class UpdatePinnedPost : IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/user/pinned-post/{pinnedPostId}";
    public HttpRequestMethod Method => HttpRequestMethod.Put;
    public Dictionary<string, string> UrlParameters { get; } = [];

    public UpdatePinnedPost(string pinnedPostId) => UrlParameters["pinnedPostId"] = pinnedPostId;
}
