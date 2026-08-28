using History.Commons.DataTypes.Contents;
using History.Commons.Enums;

namespace History.WindowsClient.ViewModels;

// Wraps a batch of consecutive media contents for the WrappedMediaContentControl.
// The control owns its own carousel view model and pulls data in through its
// dependency properties, so this wrapper only carries the raw snapshot.
public sealed partial class WrappedMediaContentItemViewModel(List<MediaContent> mediaContents, List<MediaContent> allMediaContents, PostType postType, bool isParentPost) : IContentViewModel
{
    public List<MediaContent> MediaContents { get; } = mediaContents;
    public List<MediaContent> AllMediaContents { get; } = allMediaContents;
    public PostType PostType { get; } = postType;
    public bool IsParentPost { get; } = isParentPost;
}