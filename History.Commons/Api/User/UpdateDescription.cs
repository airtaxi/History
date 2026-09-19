using History.Commons.DataTypes.RequestDtos;
using History.Commons.Interfaces;
using System.Text.Json.Serialization.Metadata;
using History.Commons.Serialization;

namespace History.Commons.Api.User;

public class UpdateDescription : IAuthRequiredRequest, IRequestWithBody
{
    public string Path => "/api/user/description";
    public HttpRequestMethod Method => HttpRequestMethod.Put;
    public object Body { get; set; }
    public JsonTypeInfo BodyTypeInfo => ApiWebJsonSerializerContext.Default.GetTypeInfo(typeof(UpdateUserDescriptionRequestDto));

    public UpdateDescription(string description) => Body = new UpdateUserDescriptionRequestDto
    {
        Description = description
    };
}
