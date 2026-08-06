using History.Commons.DataTypes.Contents;
using History.MobileClient.Enums;

namespace History.MobileClient.ViewModels;

public partial class HistoryWrappedMediaContentsViewModel : BaseWrappedMediaContentsViewModel
{
    public HistoryWrappedMediaContentsViewModel(IEnumerable<MediaContent> mediaContents, IEnumerable<MediaContent> allMediaContents, PostType postType, bool isParentPost = false)
        : base(mediaContents.Select(m => new HistoryMediaContentViewModel(m, allMediaContents, postType, isParentPost)), postType) { }
}
