using History.Commons.DataTypes.Contents;
using History.Commons.DataTypes.RequestDtos;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.Post;

public class ModifyPost : IBaseRequest<PostResponseDto>, IAuthRequiredRequest, IRequestWithForm, IRequestWithFiles, IRequestWithUrlParameters
{
    public string Path => "/api/post/{postId}";
    public Method Method => Method.Put;
    public object Body { get; set; }
    public Dictionary<string, byte[]> Files { get; set; }
    public Dictionary<string, string> UrlParameters { get; set; } = [];

    public ModifyPost(string postId, List<BaseContent> contents, DiscoveryOption discoveryOption, AccessPermission? commentPermission, bool disallowShare, List<string> discoveryOptionSelectedUserIds = null, Dictionary<string, byte[]> files = null, List<string> hashtags = null)
    {
        Body = new ModifyPostRequestDto
        {
            Contents = contents,
            DiscoveryOption = discoveryOption,
            DiscoveryOptionSelectedUserIds = discoveryOptionSelectedUserIds,
            CommentPermission = commentPermission,
            DisallowShare = disallowShare,
            Hashtags = hashtags ?? []
        };
        Files = files ?? [];
        UrlParameters["postId"] = postId;
    }
}
