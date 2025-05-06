using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.Comment;

public class DeleteComment : IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/comment/{commentId}";
    public Method Method => Method.Delete;
    public Dictionary<string, string> UrlParameters { get; set; } = [];

    public DeleteComment(string commentId) => UrlParameters["commentId"] = commentId;
}
