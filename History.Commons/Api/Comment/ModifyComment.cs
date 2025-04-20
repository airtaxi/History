using History.Commons.DataTypes.Contents;
using History.Commons.Interfaces;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.Commons.Api.Comment;

public class ModifyComment : IAuthRequiredRequest, IRequestWithUrlParameters, IRequestWithBody, IRequestWithFiles
{
    public string Path => "/api/comment/{commentId}";
    public Method Method => Method.Put;
    public Dictionary<string, string> UrlParameters { get; set; } = [];
    public object Body { get; set; }
    public Dictionary<string, byte[]> Files { get; set; }

    public ModifyComment(string commentId, List<BaseContent> contents, Dictionary<string, byte[]> files = null)
    {
        UrlParameters["commentId"] = commentId;
        Body = contents;
        Files = files ?? [];
    }
}
