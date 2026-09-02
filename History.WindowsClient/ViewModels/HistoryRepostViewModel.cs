using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;

namespace History.WindowsClient.ViewModels;

// Mirrors the MAUI HistoryRepostViewModel: a thin wrapper that renders the parent
// post with repost attribution. The template surface (RepostTemplate) reads the
// Repost* properties from BasePostViewModel.
public partial class HistoryRepostViewModel : HistoryPostViewModel
{
    private readonly UserResponseDto _repostedUser;

    public HistoryRepostViewModel(string postId, PostResponseDto parentPost, UserResponseDto repostedUser, BaseViewModel baseViewModel) : base(parentPost, PostType.Timeline, baseViewModel)
    {
        _repostedUser = repostedUser;
        RepostId = postId;
        RepostedUserNickname = repostedUser?.Nickname;
        RepostPostfix = "님이 리포스트 했어요";

        IsRepost = true;
        IsShare = false;
    }

    public override async Task HandleRepostedUserTap()
    {
        if (_repostedUser == null) return;

        // TODO: Navigate to the user profile page once it is implemented.
        await Task.CompletedTask;
    }
}