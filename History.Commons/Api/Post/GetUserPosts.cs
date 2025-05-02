using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Interfaces;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.Commons.Api.Post;

public class GetUserPosts : IBaseRequest<List<PostResponseDto>>, IOptionalAuthRequest, IRequestWithUrlParameters, IRequestWithQueryParameters
{
    public string Path => "/api/post/user/{userId}";
    public Method Method => Method.Get;
    public Dictionary<string, string> UrlParameters { get; set; } = [];
    public Dictionary<string, string> QueryParameters { get; set; } = [];

    public GetUserPosts(string userId, string fromPostId = null, int limit = 10)
    {
        UrlParameters["userId"] = userId;
        QueryParameters["limit"] = limit.ToString();
        if (fromPostId != null) QueryParameters["from"] = fromPostId;
    }

}
