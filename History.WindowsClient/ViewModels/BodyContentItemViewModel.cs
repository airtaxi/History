using History.Commons.DataTypes.Contents;
using History.Commons.Enums;

namespace History.WindowsClient.ViewModels;

// Wraps a batch of consecutive text-type contents (Text/Profile/Hashtag/Hyperlink)
// for the BodyContentControl, which decomposes them into renderable segments.
public sealed partial class BodyContentItemViewModel(List<BaseContent> textTypeContents, PostType postType, bool hasMedias, bool isParentPost) : IContentViewModel
{
    public List<BaseContent> TextTypeContents { get; } = textTypeContents;
    public PostType PostType { get; } = postType;
    public bool HasMedias { get; } = hasMedias;
    public bool IsParentPost { get; } = isParentPost;

    // Text truncation limits mirrored from the MAUI client's Utils constants.
    public int MaxTextLength => PostType == PostType.Timeline ? (HasMedias ? 80 : 400) : 1600;
    public int MaxTextLines => PostType == PostType.Timeline ? (HasMedias ? 8 : 12) : 27;

    // Text selection is only enabled for Unwrapped posts, never for the shared post's original.
    public bool IsTextSelectionEnabled => PostType == PostType.Unwrapped && !IsParentPost;
}