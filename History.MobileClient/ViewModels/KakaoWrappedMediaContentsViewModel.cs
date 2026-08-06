using History.MobileClient.Enums;
using static History.MobileClient.KakaoStory.KakaoStoryApiHandler.DataType.CommentData;

namespace History.MobileClient.ViewModels;

public partial class KakaoWrappedMediaContentsViewModel(IEnumerable<Medium> medias, PostType postType)
    : BaseWrappedMediaContentsViewModel(medias.Select(medium => new KakaoMediaContentViewModel(medium, medias, postType)), postType);
