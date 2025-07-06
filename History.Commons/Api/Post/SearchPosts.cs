using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.Post;

public class SearchPosts : IBaseRequest<List<PostResponseDto>>, IOptionalAuthRequest, IRequestWithQueryParameters
{
    public string Path => "/api/post/search";
    public Method Method => Method.Get;
    public Dictionary<string, string> QueryParameters { get; set; } = [];

    public SearchPosts(string keyword, string fromPostId = null, int limit = 10)
    {
        QueryParameters["keyword"] = keyword;
        QueryParameters["limit"] = limit.ToString();
        if (fromPostId != null) QueryParameters["fromPostId"] = fromPostId;
    }
}
