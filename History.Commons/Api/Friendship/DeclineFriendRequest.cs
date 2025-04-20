using History.Commons.Interfaces;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.Commons.Api.Friendship;

public class DeclineFriendRequest : IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/friendship/request/{userIdToDecline}/decline";
    public Method Method => Method.Post;
    public Dictionary<string, string> UrlParameters { get; set; } = [];

    public DeclineFriendRequest(string userIdToDecline) => UrlParameters["userIdToDecline"] = userIdToDecline;
}
