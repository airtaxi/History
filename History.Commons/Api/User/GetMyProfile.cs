using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Interfaces;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.Commons.Api.User;

public class GetMyProfile : IBaseRequest<UserResponseDto>, IAuthRequiredRequest
{
    public string Path => "/api/user/me";
    public Method Method => Method.Get;
}
