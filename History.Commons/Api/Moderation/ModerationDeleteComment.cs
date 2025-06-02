using History.Commons.Enums;
using History.Commons.Interfaces;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.Commons.Api.Moderation;

public class ModerationDeleteComment : IAuthRequiredRequest, IRequestWithUrlParameters, IRequestWithQueryParameters
{
    public string Path => "/api/moderation/delete-comment/{commentId}";
    public Method Method => Method.Post;
    public Dictionary<string, string> UrlParameters { get; set; } = new Dictionary<string, string>();
    public Dictionary<string, string> QueryParameters { get; set; } = new Dictionary<string, string>();

    public ModerationDeleteComment(string commentId, string reason, ReportType reportType)
    {
        UrlParameters["commentId"] = commentId;
        QueryParameters["reason"] = reason;
        QueryParameters["reportType"] = reportType.ToString();
    }
}
