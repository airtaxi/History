using History.Commons.Interfaces;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.Commons.Api.Report;

public class DeleteReportRecord : IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/report/{recordId}";
    public Method Method => Method.Delete;
    public Dictionary<string, string> UrlParameters { get; set; } = [];

    public DeleteReportRecord(string recordId) => UrlParameters["recordId"] = recordId;
}
