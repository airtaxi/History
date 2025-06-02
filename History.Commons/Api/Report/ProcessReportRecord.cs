using History.Commons.Interfaces;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.Commons.Api.Report;

public class ProcessReportRecord : IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/report/process/{recordId}";
    public Method Method => Method.Post;
    public Dictionary<string, string> UrlParameters { get; set; } = [];

    public ProcessReportRecord(string recordId) => UrlParameters["recordId"] = recordId;
}
