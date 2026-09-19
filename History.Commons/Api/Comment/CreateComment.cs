using History.Commons.DataTypes.Contents;
using History.Commons.Interfaces;
using System.Text.Json.Serialization.Metadata;
using History.Commons.Serialization;

namespace History.Commons.Api.Comment;

public class CreateComment : IAuthRequiredRequest, IRequestWithUrlParameters, IRequestWithForm, IRequestWithFiles
{
    public string Path => "/api/comment/{postId}";
    public HttpRequestMethod Method => HttpRequestMethod.Post;
    public Dictionary<string, string> UrlParameters { get; set; } = [];
    public object Body { get; set; }
    public JsonTypeInfo BodyTypeInfo => ApiJsonSerializerContext.Default.GetTypeInfo(typeof(List<BaseContent>));
    public Dictionary<string, byte[]> Files { get; set; }

    public CreateComment(string postId, List<BaseContent> contents, Dictionary<string, byte[]> files = null)
    {
        UrlParameters["postId"] = postId;
        Body = contents;
        Files = files ?? [];
    }
}
