using History.Commons.DataTypes.Contents;
using History.Commons.DataTypes.RequestDtos;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using History.Commons.Interfaces;
using System.Text.Json.Serialization.Metadata;
using History.Commons.Serialization;

namespace History.Commons.Api.Post;

public class WritePost : IBaseRequest<PostResponseDto>, IAuthRequiredRequest, IRequestWithForm, IRequestWithFiles
{
    public string Path => "/api/post";
    public HttpRequestMethod Method => HttpRequestMethod.Post;
    public object Body { get; set; }
    public JsonTypeInfo BodyTypeInfo => ApiJsonSerializerContext.Default.GetTypeInfo(typeof(WritePostRequestDto));
    public Dictionary<string, byte[]> Files { get; set; }

    public WritePost(List<BaseContent> contents, DiscoveryOption discoveryOption, AccessPermission? commentPermission, bool disallowShare, string ParentPostId = null, List<string> discoveryOptionSelectedUserIds = null, Dictionary<string, byte[]> files = null, DateTime? reservationTime = null)
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
            Hashtags = []
        };
        Files = files ?? [];
    }
}
