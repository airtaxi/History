using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.Comment;

public class GetCommentsByPostId : IBaseRequest<List<CommentResponseDto>>, IOptionalAuthRequest, IRequestWithUrlParameters, IRequestWithQueryParameters
{
    public string Path => "/api/comment/{postId}";
    public Method Method => Method.Get;
    public Dictionary<string, string> UrlParameters { get; set; } = [];
    public Dictionary<string, string> QueryParameters { get; set; } = [];

    public GetCommentsByPostId(string postId, string fromCommentId = null, int limit = 10)
    {
        UrlParameters["postId"] = postId;
        QueryParameters["limit"] = limit.ToString();
        if (fromCommentId != null) QueryParameters["from"] = fromCommentId;
    }
}
