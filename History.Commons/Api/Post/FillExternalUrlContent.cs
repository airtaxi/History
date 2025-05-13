using History.Commons.DataTypes.Contents;
using History.Commons.DataTypes.RequestDtos;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using History.Commons.Interfaces;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.Commons.Api.Post;

public class FillExternalUrlContent : IBaseRequest<ExternalUrlContent>, IAuthRequiredRequest, IRequestWithBody
{
    public string Path => "/api/post/fill-external-url-content";
    public Method Method => Method.Post;
    public object Body { get; set; }

    public FillExternalUrlContent(ExternalUrlContent externalUrlContent) => Body = externalUrlContent;
}
