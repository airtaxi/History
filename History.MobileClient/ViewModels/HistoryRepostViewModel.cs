using History.Commons.DataTypes.ResponseDtos;
using History.MobileClient.Enums;
using History.MobileClient.Pages;

namespace History.MobileClient.ViewModels;

public partial class HistoryRepostViewModel : HistoryPostViewModel
{
    private readonly UserResponseDto _repostedUser;

    public HistoryRepostViewModel(string postId, PostResponseDto parentPost, UserResponseDto repostedUser) : base(parentPost, PostType.Timeline)
    {
        _repostedUser = repostedUser;
        RepostId = postId;
        RepostedUserNickname = _repostedUser?.Nickname;
        RepostPostfix = "님이 리포스트 했어요";
    }

    public override async Task HandleRepostedUserTap()
    {
        if (_repostedUser == null) return;

        await App.PushAsync(new BlazorUserPage(_repostedUser.UserId));
    }
}
