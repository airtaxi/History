using History.Commons.Enums;
using static History.Commons.KakaoStory.KakaoStoryApiHandler.DataType.CommentData;

namespace History.MobileClient.ViewModels;

public partial class KakaoWrappedMediaContentsViewModel(IEnumerable<Medium> medias, PostType postType)
    : BaseWrappedMediaContentsViewModel(medias.Select(medium => new KakaoMediaContentViewModel(medium, medias, postType)), postType);
