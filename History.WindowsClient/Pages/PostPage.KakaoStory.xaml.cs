using History.WindowsClient.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using static History.Commons.KakaoStory.KakaoStoryApiHandler.DataType.CommentData;

namespace History.WindowsClient.Pages;

// Kakao Story post detail code-behind: initializes the Kakao page view model when the
// navigation parameter carries a Kakao Story post.
public sealed partial class PostPage
{
    private KakaoPostPageViewModel _kakaoViewModel;

    private bool TryInitializeKakaoStory(object parameter)
    {
        if (parameter is not PostData kakaoData) return false;

        _kakaoViewModel ??= App.Services.GetRequiredService<KakaoPostPageViewModel>();
        _kakaoViewModel.Initialize(kakaoData);
        _activeViewModel = _kakaoViewModel;
        return true;
    }
}
