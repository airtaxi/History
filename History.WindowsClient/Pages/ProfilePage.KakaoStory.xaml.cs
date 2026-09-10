using History.WindowsClient.Models;
using History.WindowsClient.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace History.WindowsClient.Pages;

// Kakao Story side of the profile page: resolves the navigation parameter, keeps the
// per-user view model cache and releases it when the page is left for good.
public sealed partial class ProfilePage
{
    private static readonly Dictionary<string, KakaoProfilePageViewModel> KakaoViewModelCache = [];

    private KakaoProfilePageViewModel _kakaoViewModel;

    // Switches the page to the Kakao Story profile the parameter identifies and
    // returns whether the parameter was a Kakao Story profile.
    private bool TryInitializeKakaoStory(object parameter)
    {
        if (parameter is not KakaoProfileParameters kakaoProfile) return false;

        IsKakaoStoryPage = true;

        if (!KakaoViewModelCache.TryGetValue(kakaoProfile.KakaoUserId, out var cachedViewModel))
        {
            cachedViewModel = App.Services.GetRequiredService<KakaoProfilePageViewModel>();
            cachedViewModel.Initialize(kakaoProfile.KakaoUserId);
            KakaoViewModelCache[kakaoProfile.KakaoUserId] = cachedViewModel;
        }
        else _shouldRestoreScroll = cachedViewModel.ScrollHeight > 0;

        _kakaoViewModel = cachedViewModel;
        _activeViewModel = _kakaoViewModel;
        return true;
    }

    // Releasing the Kakao cache entry happens only when the page is left for good
    // through back navigation.
    private void ReleaseKakaoViewModel()
    {
        if (ViewModel is KakaoProfilePageViewModel kakaoViewModel)
        {
            KakaoViewModelCache.Remove(kakaoViewModel.UserId);
        }
    }
}
