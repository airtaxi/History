using History.Commons.DataTypes.RequestDtos;
using History.Commons.Interfaces;
using System.Text.Json.Serialization.Metadata;
using History.Commons.Serialization;

namespace History.Commons.Api.KakaoStory;

public class UpdateKakaoStoryToken : IAuthRequiredRequest, IRequestWithBody
{
    public string Path => "/api/kakaostory/token";
    public HttpRequestMethod Method => HttpRequestMethod.Post;
    public object Body { get; set; }
    public JsonTypeInfo BodyTypeInfo => ApiWebJsonSerializerContext.Default.GetTypeInfo(typeof(UpdateKakaoStoryTokenRequestDto));

    public UpdateKakaoStoryToken(UpdateKakaoStoryTokenRequestDto requestDto) => Body = requestDto;
}
