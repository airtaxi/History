using History.Commons.DataTypes.Contents;
using History.Commons.Interfaces;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.Commons.Api.Comment;

public class CreateComment : IAuthRequiredRequest, IRequestWithUrlParameters, IRequestWithBody, IRequestWithFiles
{
    public string Path => "/api/comment/{postId}";
    public Method Method => Method.Post;
    public Dictionary<string, string> UrlParameters { get; set; } = [];
    public object Body { get; set; }
    public Dictionary<string, byte[]> Files { get; set; }

    public CreateComment(string postId, List<BaseContent> contents, Dictionary<string, byte[]> files = null)
    {
        UrlParameters["postId"] = postId;
        Body = contents;
        Files = files ?? [];
    }
}
