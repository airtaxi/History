using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Interfaces;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.Commons.Api.Post;

public class DeletePost : IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/post/{postId}";
    public Method Method => Method.Delete;
    public Dictionary<string, string> UrlParameters { get; set; } = [];

    public DeletePost(string postId) => UrlParameters["postId"] = postId;
}
