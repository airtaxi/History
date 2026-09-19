using History.Commons.DataTypes.RequestDtos;
using History.Commons.Interfaces;
using System.Text.Json.Serialization.Metadata;
using History.Commons.Serialization;

namespace History.Commons.Api.User;

public class UpdateBirthday : IAuthRequiredRequest, IRequestWithBody
{
    public string Path => "/api/user/birthday";
    public HttpRequestMethod Method => HttpRequestMethod.Put;
    public object Body { get; set; }
    public JsonTypeInfo BodyTypeInfo => ApiWebJsonSerializerContext.Default.GetTypeInfo(typeof(UpdateUserBirthdayRequestDto));

    public UpdateBirthday(DateTime? birthday) => Body = new UpdateUserBirthdayRequestDto()
    {
        Birthday = birthday
    };
}
