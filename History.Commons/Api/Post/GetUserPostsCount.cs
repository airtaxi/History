using History.Commons.DataTypes.RequestDtos;
using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.Post;

public class GetUserPostsCount : IBaseRequest<GetUserPostsCountResponseDto>, IOptionalAuthRequest, IRequestWithUrlParameters
{
    public string Path => "/api/post/{userId}/count";
    public Method Method => Method.Get;
    public Dictionary<string, string> UrlParameters { get; set; } = [];

    public GetUserPostsCount(string userId) => UrlParameters["userId"] = userId;
}
