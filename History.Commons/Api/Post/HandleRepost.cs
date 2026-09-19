using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Interfaces;

namespace History.Commons.Api.Post;

public class HandleRepost : IBaseRequest<PostResponseDto>, IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/post/{postId}/repost";
    public HttpRequestMethod Method => HttpRequestMethod.Post;
    public Dictionary<string, string> UrlParameters { get; set; } = [];

    public HandleRepost(string postId) => UrlParameters["postId"] = postId;
}
