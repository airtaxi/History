using History.Commons.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.Commons.Api.Report;

public class ProcessReportRecord : IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/report/process/{recordId}";
    public HttpRequestMethod Method => HttpRequestMethod.Post;
    public Dictionary<string, string> UrlParameters { get; set; } = [];

    public ProcessReportRecord(string recordId) => UrlParameters["recordId"] = recordId;
}
