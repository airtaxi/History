using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Interfaces;

namespace History.Commons.Api.User;

public class GetUserByHandle : IBaseRequest<UserResponseDto>, IOptionalAuthRequest, IRequestWithUrlParameters
{
    public string Path => "/api/user/handle/{handle}";
    public HttpRequestMethod Method => HttpRequestMethod.Get;
    public Dictionary<string, string> UrlParameters { get; } = [];

    public GetUserByHandle(string handle) => UrlParameters["handle"] = handle;
}
