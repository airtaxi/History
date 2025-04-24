using CommunityToolkit.Mvvm.ComponentModel;
using History.Commons;
using History.Commons.Api.User;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.MobileClient.ViewModels;

public partial class ProfileViewModel(UserResponseDto user) : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMe))]
    [NotifyPropertyChangedFor(nameof(IsNotMe))]
    [NotifyPropertyChangedFor(nameof(FriendButtonText))]
    [NotifyPropertyChangedFor(nameof(Nickname))]
    [NotifyPropertyChangedFor(nameof(Description))]
    [NotifyPropertyChangedFor(nameof(FriendshipDescription))]
    [NotifyPropertyChangedFor(nameof(BackgroundMedia))]
    [NotifyPropertyChangedFor(nameof(ProfileMedia))]
    public partial UserResponseDto User { get; set; } = user;

    public bool IsMe => User.UserId == Shared.UserId;
    public bool IsNotMe => User.UserId != Shared.UserId;

    public string FriendButtonText
    {
        get
        {
            if (IsMe) return "ERROR";
            else if (User.Friendship != null && User.Friendship.Status == FriendshipStatus.Accepted)
            {
                return "친구 삭제";
            }
            else if (User.Friendship != null && User.Friendship.Status == FriendshipStatus.Requested)
            {
                return "친구 요청 취소";
            }
            else return "친구 추가";
        }
    }

    public string Nickname => User.Nickname;
    public string Description => string.IsNullOrEmpty(User.Description) ? "설정된 한줄 소개가 없습니다" : User.Description;
    public string FriendshipDescription
    {
        get
        {
            if (IsMe) return "내 프로필입니다.";
            else if (User.Friendship != null && User.Friendship.Status == FriendshipStatus.Accepted)
            {
                var friendDays = (DateTime.UtcNow - User.Friendship.CreatedAt).TotalDays;
                if (friendDays < 1)
                    return "친구가 된지 하루도 안됐어요!";
                else if (friendDays < 30)
                    return $"{friendDays}일째 친구에요!";
                else if (friendDays < 365)
                    return $"{friendDays / 30}개월째 친구에요!";
                else
                    return $"{friendDays / 365}년째 친구에요!";
            }
            else if (User.Friendship != null && User.Friendship.Status == FriendshipStatus.Requested)
            {
                return "친구 요청을 보냈어요!";
            }
            else return "친구가 아니에요.";
        }
    }

    public IMediaViewModel BackgroundMedia => User.UsesAnimatedBackgroundMedia ? new VideoViewModel(Utils.GenerateMediaUri(User.BackgroundMediaId)) : new ImageViewModel(Utils.GenerateMediaUri(User.BackgroundMediaId) ?? "icon.png");
    public IMediaViewModel ProfileMedia => User.UsesAnimatedBackgroundMedia ? new VideoViewModel(Utils.GenerateMediaUri(User.ProfileMediaId)) : new ImageViewModel(Utils.GenerateMediaUri(User.ProfileMediaId) ?? "default_profile_image.jpg");

    private async Task RefreshAsync() => User = await Shared.ApiHandler.ExecuteRequestAsync(new GetUser(User.UserId));

    public async void OnEditNicknameImageTapped(object sender, TappedEventArgs e)
    {
        var result = await App.MainWindow.Page.DisplayPromptAsync("닉네임 변경", "새로운 닉네임을 입력해주세요", "변경", "취소", "새로운 닉네임", 40, Keyboard.Plain, User.Nickname);
        result = result?.Trim();

        if (result != null && result != User.Nickname)
        {
            if (string.IsNullOrWhiteSpace(result))
            {
                await App.MainWindow.Page.DisplayAlert("닉네임 변경 실패", "닉네임은 공백으로 설정할 수 없습니다", "확인");
                return;
            }
            else if (result.Length > CommonsConstants.MaxNicknameLength)
            {
                await App.MainWindow.Page.DisplayAlert("닉네임 변경 실패", $"닉네임은 {CommonsConstants.MaxNicknameLength}자 이하로 설정할 수 있습니다", "확인");
                return;
            }

            await Shared.ApiHandler.ExecuteRequestAsync(new UpdateNickname(result));
            await RefreshAsync();
        }
    }

    public async void OnEditDescriptionImageTapped(object sender, TappedEventArgs e)
    {
        var result = await App.MainWindow.Page.DisplayPromptAsync("한줄 소개 변경", "새로운 한줄 소개를 입력해주세요 (공백 시 설정 해제)", "변경", "취소", "새로운 한줄 소개 (공백 시 설정 해제)", 40, Keyboard.Plain, User.Description);
        result = result?.Trim();

        if (result != null && result != User.Description)
        {
            if (result.Length > CommonsConstants.MaxDescriptionLength)
            {
                await App.MainWindow.Page.DisplayAlert("한줄 소개 변경 실패", $"한줄 소개는 {CommonsConstants.MaxDescriptionLength}자 이하로 설정할 수 있습니다", "확인");
                return;
            }

            await Shared.ApiHandler.ExecuteRequestAsync(new UpdateDescription(result));
            await RefreshAsync();
        }
    }

}
