using History.Commons.Interfaces;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.Commons.Api.Friendship;

public class RemoveFriend : IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/friendship/remove/{userIdToRemove}";
    public Method Method => Method.Post;
    public Dictionary<string, string> UrlParameters { get; set; } = [];

    public RemoveFriend(string userIdToRemove) => UrlParameters["userIdToRemove"] = userIdToRemove;
}
