using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.Commons.Api.Moderation
{
    public class GetModerationRecords : IBaseRequest<List<ModerationRecordResponseDto>>, IAuthRequiredRequest, IRequestWithQueryParameters
    {
        public string Path => "api/moderation/records";
        public HttpRequestMethod Method => HttpRequestMethod.Get;
        public Dictionary<string, string> QueryParameters { get; set; } = [];

        public GetModerationRecords(string from = null, int limit = 10)
        {
            if (!string.IsNullOrEmpty(from)) QueryParameters["from"] = from;
            QueryParameters["limit"] = limit.ToString();
        }
    }
}
