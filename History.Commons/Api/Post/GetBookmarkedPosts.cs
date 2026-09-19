using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Interfaces;

namespace History.Commons.Api.Post;

public class GetBookmarkedPosts : IBaseRequest<List<PostResponseDto>>, IAuthRequiredRequest, IRequestWithQueryParameters
{
    public string Path => "/api/post/bookmarks";
    public HttpRequestMethod Method => HttpRequestMethod.Get;
    public Dictionary<string, string> QueryParameters { get; set; } = [];

    public GetBookmarkedPosts(string fromPostId = null, int limit = 20)
    {
        QueryParameters["limit"] = limit.ToString();
        if (fromPostId != null) QueryParameters["from"] = fromPostId;
    }
}
