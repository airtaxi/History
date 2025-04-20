using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Interfaces;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.Commons.Api.User;

public class GetUser : IBaseRequest<UserResponseDto>, IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/user/{userId}";
    public Method Method => Method.Get;
    public Dictionary<string, string> UrlParameters { get; set; } = [];

    public GetUser(string userId) => UrlParameters["userId"] = userId;
}
