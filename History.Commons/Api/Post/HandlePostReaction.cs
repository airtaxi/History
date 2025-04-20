using History.Commons.Enums;
using History.Commons.Interfaces;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.Commons.Api.Post;

public class HandlePostReaction : IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/post/{postId}/reaction/{type}";
    public Method Method => Method.Post;
    public Dictionary<string, string> UrlParameters { get; set; } = [];

    public HandlePostReaction(string postId, PostReactionType type)
    {
        UrlParameters["postId"] = postId;
        UrlParameters["type"] = type.ToString();
    }
}
