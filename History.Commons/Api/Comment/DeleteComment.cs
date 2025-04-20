using History.Commons.Interfaces;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.Commons.Api.Comment;

public class DeleteComment : IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/comment/{commentId}";
    public Method Method => Method.Delete;
    public Dictionary<string, string> UrlParameters { get; set; } = [];

    public DeleteComment(string commentId) => UrlParameters["commentId"] = commentId;
}
