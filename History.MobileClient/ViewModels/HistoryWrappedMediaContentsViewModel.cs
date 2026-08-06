using History.Commons.DataTypes.Contents;
using History.MobileClient.Enums;

namespace History.MobileClient.ViewModels;

public partial class HistoryWrappedMediaContentsViewModel(IEnumerable<MediaContent> mediaContents, IEnumerable<MediaContent> allMediaContents, PostType postType, bool isParentPost = false)
    : BaseWrappedMediaContentsViewModel(mediaContents.Select(m => new HistoryMediaContentViewModel(m, allMediaContents, postType, isParentPost)), postType);
