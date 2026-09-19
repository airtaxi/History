using History.Commons.Interfaces;

namespace History.Commons.Api.Comment;

public class DeleteComment : IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/comment/{commentId}";
    public HttpRequestMethod Method => HttpRequestMethod.Delete;
    public Dictionary<string, string> UrlParameters { get; set; } = [];

    public DeleteComment(string commentId) => UrlParameters["commentId"] = commentId;
}
