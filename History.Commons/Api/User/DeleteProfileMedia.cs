using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.User;

public class DeleteProfileMedia : IAuthRequiredRequest
{
    public string Path => "/api/user/profile-media";
    public Method Method => Method.Delete;
}
