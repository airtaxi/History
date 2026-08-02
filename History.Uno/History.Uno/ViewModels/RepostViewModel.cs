using CommunityToolkit.Mvvm.Input;
using History.Commons.DataTypes.ResponseDtos;
using History.Uno.Enums;

namespace History.Uno.ViewModels;

public partial class RepostViewModel(string postId, PostResponseDto parentPost, UserResponseDto repostedUser) : PostViewModel(parentPost, PostType.Timeline)
{
    public string RepostedUserNickname => repostedUser?.Nickname;
    public string RepostId { get; } = postId;

    [RelayCommand]
    public async Task HandleRepostedUserTap()
    {
        if (repostedUser == null) return;

        await App.PushAsync(typeof(Pages.UserPage), repostedUser.UserId);
    }
}
