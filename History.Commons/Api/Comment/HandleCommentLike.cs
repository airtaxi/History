using History.Commons.Interfaces;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.Commons.Api.Comment;

public class HandleCommentLike : IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/comment/{commentId}/like";
    public Method Method => Method.Post;
    public Dictionary<string, string> UrlParameters { get; set; } = [];

    public HandleCommentLike(string commentId) => UrlParameters["commentId"] = commentId;
}
