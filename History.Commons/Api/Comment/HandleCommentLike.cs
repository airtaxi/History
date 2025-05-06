using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.Comment;

public class HandleCommentLike : IBaseRequest<CommentResponseDto>, IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/comment/{commentId}/like";
    public Method Method => Method.Post;
    public Dictionary<string, string> UrlParameters { get; set; } = [];

    public HandleCommentLike(string commentId) => UrlParameters["commentId"] = commentId;
}
