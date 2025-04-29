using History.Commons.DataTypes.Contents;
using History.Commons.Enums;

namespace History.Commons.DataTypes.RequestDtos;

public class WritePostRequestDto
{
    public DiscoveryOption DiscoveryOption { get; set; }
    public List<BaseContent> Contents { get; set; } = [];
    public string ParentPostId { get; set; }
    public List<string> DiscoveryOptionSelectedUserIds { get; set; } = [];
}
