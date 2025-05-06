using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.User;

public class DeleteBackgroundMedia : IAuthRequiredRequest
{
    public string Path => "/api/user/background-media";
    public Method Method => Method.Delete;
}
