using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Interfaces;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.Commons.Api.Post;

public class GetTimelinePosts : IBaseRequest<List<PostResponseDto>>, IOptionalAuthRequest, IRequestWithQueryParameters
{
    public string Path => "/api/post/timeline";
    public Method Method => Method.Get;
    public Dictionary<string, string> QueryParameters { get; set; } = [];

    public GetTimelinePosts(string fromPostId = null, int limit = 10)
    {
        QueryParameters["limit"] = limit.ToString();
        if (fromPostId != null) QueryParameters["from"] = fromPostId;
    }

}
