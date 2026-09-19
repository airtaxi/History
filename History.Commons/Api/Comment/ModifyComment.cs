using History.Commons.DataTypes.Contents;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Interfaces;
using System.Text.Json.Serialization.Metadata;
using History.Commons.Serialization;

namespace History.Commons.Api.Comment;

public class ModifyComment : IBaseRequest<CommentResponseDto>, IAuthRequiredRequest, IRequestWithUrlParameters, IRequestWithForm, IRequestWithFiles
{
    public string Path => "/api/comment/{commentId}";
    public HttpRequestMethod Method => HttpRequestMethod.Put;
    public Dictionary<string, string> UrlParameters { get; set; } = [];
    public object Body { get; set; }
    public JsonTypeInfo BodyTypeInfo => ApiJsonSerializerContext.Default.GetTypeInfo(typeof(List<BaseContent>));
    public Dictionary<string, byte[]> Files { get; set; }

    public ModifyComment(string commentId, List<BaseContent> contents, Dictionary<string, byte[]> files = null)
    {
        UrlParameters["commentId"] = commentId;
        Body = contents;
        Files = files ?? [];
    }
}
