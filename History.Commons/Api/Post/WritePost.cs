using History.Commons.DataTypes.Contents;
using History.Commons.DataTypes.RequestDtos;
using History.Commons.Enums;
using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.Post;

public class WritePost : IAuthRequiredRequest, IRequestWithForm, IRequestWithFiles
{
    public string Path => "/api/post";
    public Method Method => Method.Post;
    public object Body { get; set; }
    public Dictionary<string, byte[]> Files { get; set; }

    public WritePost(List<BaseContent> contents, DiscoveryOption discoveryOption, AccessPermission? commentPermission, bool disallowShare, string ParentPostId = null, List<string> discoveryOptionSelectedUserIds = null, Dictionary<string, byte[]> files = null, DateTime? reservationTime = null, List<string> hashtags = null)
    {
        Body = new WritePostRequestDto
        {
            Contents = contents,
            DiscoveryOption = discoveryOption,
            ParentPostId = ParentPostId,
            DiscoveryOptionSelectedUserIds = discoveryOptionSelectedUserIds,
            CommentPermission = commentPermission,
            ReservationTime = reservationTime,
            DisallowShare = disallowShare,
            Hashtags = hashtags ?? []
        };
        Files = files ?? [];
    }
}
