using History.Commons.DataTypes.Contents;
using History.Commons.DataTypes.RequestDtos;
using History.Commons.Interfaces;
using System.Text.Json.Serialization.Metadata;
using History.Commons.Serialization;

namespace History.Commons.Api.Message;

public class SendMessage(SendMessageRequestDto requestDto, Dictionary<string, byte[]> files = null) : IAuthRequiredRequest, IRequestWithForm, IRequestWithFiles
{
    public string Path => "/api/message/send";
    public HttpRequestMethod Method => HttpRequestMethod.Post;
    public object Body { get; set; } = requestDto;
    public JsonTypeInfo BodyTypeInfo => ApiJsonSerializerContext.Default.GetTypeInfo(typeof(SendMessageRequestDto));
    public Dictionary<string, byte[]> Files { get; set; } = files ?? [];

    public SendMessage(string receiverId, List<BaseContent> contents, Dictionary<string, byte[]> files = null)
        : this(new SendMessageRequestDto { ReceiverId = receiverId, Contents = contents }, files) { }
}