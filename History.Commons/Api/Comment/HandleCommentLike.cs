using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Interfaces;

namespace History.Commons.Api.Comment;

public class HandleCommentLike : IBaseRequest<CommentResponseDto>, IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/comment/{commentId}/like";
    public HttpRequestMethod Method => HttpRequestMethod.Post;
    public Dictionary<string, string> UrlParameters { get; set; } = [];

    public HandleCommentLike(string commentId) => UrlParameters["commentId"] = commentId;
}
