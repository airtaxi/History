using History.Commons.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.Commons.Api.PushNotification;

public class RegisterFirebaseToken : IAuthRequiredRequest, IRequestWithQueryParameters
{
    public string Path => "/api/user/register-firebase-token";
    public HttpRequestMethod Method => HttpRequestMethod.Post;
    public Dictionary<string, string> QueryParameters { get; set; } = [];

    public RegisterFirebaseToken(string token) => QueryParameters["token"] = token;
}
