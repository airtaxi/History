using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.User;

public class GetMyProfile : IBaseRequest<UserResponseDto>, IAuthRequiredRequest
{
    public string Path => "/api/user/me";
    public Method Method => Method.Get;
}
