using History.Commons.Interfaces;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.Commons.Api.Friendship;

public class UnblockUser : IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/friendship/block/{blockedUserId}";
    public Method Method => Method.Delete;
    public Dictionary<string, string> UrlParameters { get; set; } = [];

    public UnblockUser(string blockedUserId) => UrlParameters["blockedUserId"] = blockedUserId;
}
