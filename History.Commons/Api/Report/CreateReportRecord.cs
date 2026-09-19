using History.Commons.DataTypes.RequestDtos;
using History.Commons.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization.Metadata;
using History.Commons.Serialization;

namespace History.Commons.Api.Report;

public class CreateReportRecord(CreateReportRecordRequestDto requestDto) : IAuthRequiredRequest, IRequestWithBody
{
    public string Path => "/api/report";
    public HttpRequestMethod Method => HttpRequestMethod.Post;
    public object Body { get; set; } = requestDto;
    public JsonTypeInfo BodyTypeInfo => ApiWebJsonSerializerContext.Default.GetTypeInfo(typeof(CreateReportRecordRequestDto));
}
