using History.Commons.DataTypes.RequestDtos;
using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.KakaoStory;

public class UpdateKakaoStoryToken : IAuthRequiredRequest, IRequestWithBody
{
    public string Path => "/api/kakaostory/token";
    public Method Method => Method.Post;
    public object Body { get; set; }

    public UpdateKakaoStoryToken(UpdateKakaoStoryTokenRequestDto requestDto) => Body = requestDto;
}
