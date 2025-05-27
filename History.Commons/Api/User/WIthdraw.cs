using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.User;

public class Withdraw : IAuthRequiredRequest
{
    public string Path => "/api/user/withdraw";
    public Method Method => Method.Post;
}
