using History.Commons.Interfaces;
using History.Commons.DataTypes.RequestDtos;
using RestSharp;

namespace History.Commons.Api.User;

public class UnapproveUnauthorizedUser : IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/user/unapprove/{userId}";
    public Method Method => Method.Post;
    public Dictionary<string, string> UrlParameters { get; } = [];

    public UnapproveUnauthorizedUser(string userId) => UrlParameters["userId"] = userId;
}
