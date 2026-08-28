using History.Commons.DataTypes.Contents;

namespace History.WindowsClient.ViewModels;

// Wraps a poll content for the PollContentControl, which owns a PollContentViewModel
// and receives data through its dependency properties.
public sealed partial class PollContentItemViewModel(PollContent pollContent, string postId) : IContentViewModel
{
    public PollContent PollContent { get; } = pollContent;
    public string PostId { get; } = postId;
}