using History.Commons.Interfaces;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.Commons.Api.Moderation
{
    public class GetModerationRecords : IAuthRequiredRequest, IRequestWithQueryParameters
    {
        public string Path => "api/moderation/records";
        public Method Method => Method.Get;
        public Dictionary<string, string> QueryParameters { get; set; } = [];

        public GetModerationRecords(string from = null, int limit = 10)
        {
            if (!string.IsNullOrEmpty(from)) QueryParameters["from"] = from;
            QueryParameters["limit"] = limit.ToString();
        }
    }
}
