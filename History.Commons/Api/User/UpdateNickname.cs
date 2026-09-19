using History.Commons.DataTypes.RequestDtos;
using History.Commons.Interfaces;
using System.Text.Json.Serialization.Metadata;
using History.Commons.Serialization;

namespace History.Commons.Api.User;

public class UpdateNickname : IAuthRequiredRequest, IRequestWithBody
{
    public string Path => "/api/user/nickname";
    public HttpRequestMethod Method => HttpRequestMethod.Put;
    public object Body { get; set; }
    public JsonTypeInfo BodyTypeInfo => ApiWebJsonSerializerContext.Default.GetTypeInfo(typeof(UpdateUserNicknameRequestDto));

    public UpdateNickname(string nickname) => Body = new UpdateUserNicknameRequestDto
    {
        Nickname = nickname
    };
}
