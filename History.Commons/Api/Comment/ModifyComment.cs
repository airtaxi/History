using History.Commons.DataTypes.Contents;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.Comment;

public class ModifyComment : IBaseRequest<CommentResponseDto>, IAuthRequiredRequest, IRequestWithUrlParameters, IRequestWithForm, IRequestWithFiles
{
    public string Path => "/api/comment/{commentId}";
    public Method Method => Method.Put;
    public Dictionary<string, string> UrlParameters { get; set; } = [];
    public object Body { get; set; }
    public Dictionary<string, byte[]> Files { get; set; }

    public ModifyComment(string commentId, List<BaseContent> contents, Dictionary<string, byte[]> files = null)
    {
        UrlParameters["commentId"] = commentId;
        Body = contents;
        Files = files ?? [];
    }
}
