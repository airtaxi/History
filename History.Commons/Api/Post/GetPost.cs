using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Interfaces;

namespace History.Commons.Api.Post;

public class GetPost : IBaseRequest<PostResponseDto>, IOptionalAuthRequest, IRequestWithUrlParameters
{
    public string Path => "/api/post/{postId}";
    public HttpRequestMethod Method => HttpRequestMethod.Get;
    public Dictionary<string, string> UrlParameters { get; set; } = [];

    public GetPost(string postId) => UrlParameters["postId"] = postId;
}
