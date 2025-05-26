using CommunityToolkit.Mvvm.Input;
using History.Commons.DataTypes.ResponseDtos;
using History.MobileClient.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.MobileClient.ViewModels;

public partial class RepostViewModel(string postId, PostResponseDto parentPost, UserResponseDto repostedUser) : PostViewModel(parentPost, true)
{
    public string RepostedUserNickname => repostedUser?.Nickname;
    public string RepostId { get; } = postId;

    [RelayCommand]
    public async Task HandleRepostedUserTap()
    {
        if (repostedUser == null) return;

        await App.PushModalAsync(new UserPage(repostedUser.UserId));
    }
}
