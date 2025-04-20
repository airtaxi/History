using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Interfaces;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.Commons.Api.Post;

public class GetPost : IBaseRequest<PostResponseDto>, IOptionalAuthRequest, IRequestWithUrlParameters
{
    public string Path => "/api/post/{postId}";
    public Method Method => Method.Get;
    public Dictionary<string, string> UrlParameters { get; set; } = [];

    public GetPost(string postId) => UrlParameters["postId"] = postId;
}
