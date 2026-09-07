using History.Commons.DataTypes.Contents;

namespace History.Rendering;

// Image data carried by the host app for the bundled resources PostImageRenderer
// uses (the text font stream and the default profile image bytes).
public sealed class PostImageRendererResources
{
    public byte[] FontBytes { get; init; }
    public byte[] DefaultProfileImageBytes { get; init; }
}

// Post header surface for image export (profile image, nickname, timestamp).
public sealed class PostRenderHeader
{
    public string ProfileImageUrl { get; init; }
    public string Nickname { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ModifiedAt { get; init; }
}

// Comment surface for image export (avatar, nickname, raw contents, timestamp).
public sealed class CommentRenderData
{
    public string ProfileImageUrl { get; init; }
    public string Nickname { get; init; }
    public List<BaseContent> Contents { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ModifiedAt { get; init; }
}
