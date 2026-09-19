using History.Commons.DataTypes.RequestDtos;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Interfaces;
using System.Text.Json.Serialization.Metadata;
using History.Commons.Serialization;

namespace History.Commons.Api.Post;

public class VotePoll : IAuthRequiredRequest, IRequestWithUrlParameters, IRequestWithBody, IBaseRequest<PostResponseDto>
{
    public string Path => "/api/post/{postId}/poll/{pollId}/vote";
    public HttpRequestMethod Method => HttpRequestMethod.Post;
    public Dictionary<string, string> UrlParameters { get; set; } = [];
    public object Body { get; set; }
    public JsonTypeInfo BodyTypeInfo => ApiWebJsonSerializerContext.Default.GetTypeInfo(typeof(VotePollRequestDto));

    public VotePoll(string postId, string pollId, List<int> selectedOptionIndices)
    {
        UrlParameters["postId"] = postId;
        UrlParameters["pollId"] = pollId;
        Body = new VotePollRequestDto { SelectedOptionIndices = selectedOptionIndices };
    }
}
