using History.Commons.DataTypes.Contents;
using History.Commons.DataTypes.RequestDtos;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using History.Commons.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization.Metadata;
using History.Commons.Serialization;

namespace History.Commons.Api.Post;

public class FillExternalUrlContent(ExternalUrlContent externalUrlContent) : IBaseRequest<ExternalUrlContent>, IAuthRequiredRequest, IRequestWithBody
{
    public string Path => "/api/post/fill-external-url-content";
    public HttpRequestMethod Method => HttpRequestMethod.Post;
    public object Body { get; set; } = externalUrlContent;
    public JsonTypeInfo BodyTypeInfo => ApiWebJsonSerializerContext.Default.GetTypeInfo(typeof(ExternalUrlContent));
}
