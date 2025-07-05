using History.Commons.DataTypes.Contents;
using History.Commons.Enums;

namespace History.Commons.DataTypes.RequestDtos;

public class ModifyPostRequestDto
{
    public DiscoveryOption DiscoveryOption { get; set; }
    public List<BaseContent> Contents { get; set; } = [];
    public List<string> DiscoveryOptionSelectedUserIds { get; set; } = [];

    public bool DisallowShare { get; set; }

    public AccessPermission? CommentPermission { get; set; }

    public List<string> Hashtags { get; set; } = [];
}
