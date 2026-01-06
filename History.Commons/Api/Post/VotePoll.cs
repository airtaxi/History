using History.Commons.DataTypes.RequestDtos;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.Post;

public class VotePoll : IAuthRequiredRequest, IRequestWithUrlParameters, IRequestWithBody, IBaseRequest<PostResponseDto>
{
    public string Path => "/api/post/{postId}/poll/{pollId}/vote";
    public Method Method => Method.Post;
    public Dictionary<string, string> UrlParameters { get; set; } = [];
    public object Body { get; set; }

    public VotePoll(string postId, string pollId, List<int> selectedOptionIndices)
    {
        UrlParameters["postId"] = postId;
        UrlParameters["pollId"] = pollId;
        Body = new VotePollRequestDto { SelectedOptionIndices = selectedOptionIndices };
    }
}
