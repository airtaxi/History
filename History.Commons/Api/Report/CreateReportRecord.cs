using History.Commons.DataTypes.RequestDtos;
using History.Commons.Interfaces;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.Commons.Api.Report;

public class CreateReportRecord(CreateReportRecordRequestDto requestDto) : IAuthRequiredRequest, IRequestWithBody
{
    public string Path => "/api/report";
    public Method Method => Method.Post;
    public object Body { get; set; } = requestDto;
}
