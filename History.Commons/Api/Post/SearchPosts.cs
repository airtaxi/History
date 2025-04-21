using History.Commons.DataTypes.RequestDtos;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Interfaces;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.Commons.Api.Post;

public class SearchPosts : IBaseRequest<List<PostResponseDto>>, IOptionalAuthRequest, IRequestWithQueryParameters
{
    public string Path => "/api/post/search";
    public Method Method => Method.Get;
    public Dictionary<string, string> QueryParameters { get; set; } = [];

    public SearchPosts(string keyword, int limit = 10, string fromPostId = null)
    {
        QueryParameters["keyword"] = keyword;
        QueryParameters["limit"] = limit.ToString();
        if (fromPostId != null) QueryParameters["fromPostId"] = fromPostId;
    }
}
