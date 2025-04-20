using History.Commons.Interfaces;
using History.Commons.DataTypes.RequestDtos;
using RestSharp;

namespace History.Commons.Api.User;

public class MakeUserModerator : IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/user/make-moderator/{userId}";
    public Method Method => Method.Post;
    public Dictionary<string, string> UrlParameters { get; } = new();

    public MakeUserModerator(string userId) => UrlParameters["userId"] = userId;
}
