using History.Commons.DataTypes.RequestDtos;
using History.Commons.Interfaces;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.Commons.Api.Post;

public class IgnorePost : IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/post/ignore/{postId}";
    public Method Method => Method.Post;
    public Dictionary<string, string> UrlParameters { get; set; } = [];

    public IgnorePost(string postId) => UrlParameters["postId"] = postId;
}
