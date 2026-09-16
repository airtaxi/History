using CommunityToolkit.Mvvm.Input;
using History.WindowsClient.Enums;
using History.WindowsClient.Pages.Extras;

namespace History.WindowsClient.ViewModels.Extras;

// Kakao Story extras page state: exposes the batch friend cleanup and the story management
// entries that the shared batch page handles.
public sealed partial class KakaoStoryExtrasPageViewModel : BaseViewModel
{
    [RelayCommand]
    private void BatchDeleteFriends() => RequestNavigation(typeof(BatchDeleteFriendsPage));

    [RelayCommand]
    private void BatchDeletePosts() => RequestNavigation(typeof(BatchManageKakaoPostsPage), KakaoStoryBatchManageMode.Delete);

    [RelayCommand]
    private void BatchChangePermission() => RequestNavigation(typeof(BatchManageKakaoPostsPage), KakaoStoryBatchManageMode.ChangePermission);
}
