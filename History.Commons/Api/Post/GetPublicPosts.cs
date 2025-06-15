using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.Post;

public class GetPublicPosts : IBaseRequest<List<PostResponseDto>>, IOptionalAuthRequest, IRequestWithQueryParameters
{
    public string Path => "/api/post/public-post";
    public Method Method => Method.Get;
    public Dictionary<string, string> QueryParameters { get; set; } = [];

    public GetPublicPosts(string fromPostId = null, int limit = 10)
    {
        QueryParameters["limit"] = limit.ToString();
        if (fromPostId != null) QueryParameters["from"] = fromPostId;
    }
}