using History.MobileClient.Enums;
using static History.MobileClient.KakaoStory.KakaoStoryApiHandler.DataType.CommentData;

namespace History.MobileClient.ViewModels;

public partial class KakaoWrappedMediaContentsViewModel : BaseWrappedMediaContentsViewModel
{
    public KakaoWrappedMediaContentsViewModel(IEnumerable<Medium> medias, PostType postType)
        : base(medias.Select(medium => new KakaoMediaContentViewModel(medium, medias, postType)), postType) { }
}
