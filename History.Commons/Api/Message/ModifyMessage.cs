using History.Commons.DataTypes.Contents;
using History.Commons.DataTypes.RequestDtos;
using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.Message;

public class ModifyMessage : IAuthRequiredRequest, IRequestWithForm, IRequestWithFiles, IRequestWithUrlParameters
{
    public string Path => "/api/message/{messageId}";
    public Method Method => Method.Put;
    public Dictionary<string, string> UrlParameters { get; set; } = [];
    public object Body { get; set; }
    public Dictionary<string, byte[]> Files { get; set; }

    public ModifyMessage(string messageId, ModifyMessageRequestDto requestDto, Dictionary<string, byte[]> files = null)
    {
        UrlParameters["messageId"] = messageId;
        Body = requestDto;
        Files = files ?? [];
    }

    public ModifyMessage(string messageId, List<BaseContent> contents, Dictionary<string, byte[]> files = null)
        : this(messageId, new ModifyMessageRequestDto { Contents = contents }, files)
    {
    }
}