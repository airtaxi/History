using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.Post;

public class HandleRepost : IBaseRequest<PostResponseDto>, IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/post/{postId}/repost";
    public Method Method => Method.Post;
    public Dictionary<string, string> UrlParameters { get; set; } = [];

    public HandleRepost(string postId) => UrlParameters["postId"] = postId;
}
