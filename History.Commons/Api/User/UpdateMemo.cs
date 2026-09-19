using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using History.Commons.DataTypes.RequestDtos;
using History.Commons.Interfaces;
using System.Text.Json.Serialization.Metadata;
using History.Commons.Serialization;

namespace History.Commons.Api.User
{
    public class UpdateMemo : IAuthRequiredRequest, IRequestWithUrlParameters, IRequestWithBody
    {
        public string Path => "/api/user/memo/{userId}";
        public HttpRequestMethod Method => HttpRequestMethod.Put;
        public Dictionary<string, string> UrlParameters { get; } = [];
        public object Body { get; }
        public JsonTypeInfo BodyTypeInfo => ApiWebJsonSerializerContext.Default.GetTypeInfo(typeof(UpdateMemoRequestDto));

        public UpdateMemo(string userId, string memo)
        {
            UrlParameters["userId"] = userId;
            Body = new UpdateMemoRequestDto() { Memo = memo };
        }
    }
}
