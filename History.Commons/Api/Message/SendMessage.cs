using History.Commons.DataTypes.Contents;
using History.Commons.DataTypes.RequestDtos;
using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.Message;

public class SendMessage : IAuthRequiredRequest, IRequestWithForm, IRequestWithFiles
{
    public string Path => "/api/message/send";
    public Method Method => Method.Post;
    public object Body { get; set; }
    public Dictionary<string, byte[]> Files { get; set; }

    public SendMessage(SendMessageRequestDto requestDto, Dictionary<string, byte[]> files = null)
    {
        Body = requestDto;
        Files = files ?? [];
    }

    public SendMessage(string receiverId, List<BaseContent> contents, Dictionary<string, byte[]> files = null)
        : this(new SendMessageRequestDto { ReceiverId = receiverId, Contents = contents }, files)
    {
    }
}