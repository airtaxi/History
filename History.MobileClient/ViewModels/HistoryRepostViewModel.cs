using CommunityToolkit.Mvvm.Input;
using History.Commons.DataTypes.ResponseDtos;
using History.MobileClient.Enums;
using History.MobileClient.Pages;

namespace History.MobileClient.ViewModels;

public partial class HistoryRepostViewModel(string postId, PostResponseDto parentPost, UserResponseDto repostedUser) : HistoryPostViewModel(parentPost, PostType.Timeline)
{
    public string RepostedUserNickname => repostedUser?.Nickname;
    public string RepostId { get; } = postId;

    [RelayCommand]
    public async Task HandleRepostedUserTap()
    {
        if (repostedUser == null) return;

        await App.PushAsync(new UserPage(repostedUser.UserId));
    }
}
