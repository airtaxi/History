using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.User;

public class GetUserByHandle : IBaseRequest<UserResponseDto>, IOptionalAuthRequest, IRequestWithUrlParameters
{
    public string Path => "/api/user/handle/{handle}";
    public Method Method => Method.Get;
    public Dictionary<string, string> UrlParameters { get; } = new();

    public GetUserByHandle(string handle) => UrlParameters["handle"] = handle;
}
