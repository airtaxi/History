using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.Post;

/// <summary>
/// 특정 투표 옵션에 투표한 사용자 목록을 조회합니다.
/// </summary>
public class GetPollVoters(string postId, string pollId, int optionIndex) : IBaseRequest<List<PollVoterResponseDto>>, IAuthRequiredRequest, IRequestWithQueryParameters
{
    public string Path => $"/api/post/{postId}/poll/{pollId}/voters";
    public Method Method => Method.Get;
    public Dictionary<string, string> QueryParameters { get; set; } = new()
    {
        ["optionIndex"] = optionIndex.ToString()
    };
}
