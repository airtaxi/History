using CommunityToolkit.Mvvm.ComponentModel;
using History.Commons;
using History.Commons.Api.User;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using NativeMedia;

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
    public IMediaViewModel ProfileMedia => User.UsesAnimatedProfileMedia ? new VideoViewModel(Utils.GenerateMediaUri(User.ProfileMediaId)) : new ImageViewModel(Utils.GenerateMediaUri(User.ProfileMediaId) ?? "default_profile_image.jpg");

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

    public async Task HandleChangeBackgroundMediaAsync()
    {
        bool shouldUpload = true;
        if (User.BackgroundMediaId != null)
        {
            var result = await App.MainWindow.Page.DisplayActionSheet("배경 이미지", "취소", null, ["배경 이미지 변경", "배경 이미지 삭제"]);
            if (result == "취소") return;
            else if (result == "배경 이미지 변경") shouldUpload = true;
            else if (result == "배경 이미지 삭제")
            {
                try
                {
                    App.MainWindow.Page.IsEnabled = false;
                    App.MainWindow.Page.IsBusy = true;

                    await Shared.ApiHandler.ExecuteRequestAsync(new DeleteBackgroundMedia());
                    await RefreshAsync();
                    return;
                }
                finally
                {
                    App.MainWindow.Page.IsEnabled = true;
                    App.MainWindow.Page.IsBusy = false;
                }
            }
        }

        if (shouldUpload)
        {
            var request = new MediaPickRequest(1, MediaFileType.Image, MediaFileType.Video)
            {
                Title = "배경 이미지 선택"
            };

            var results = await MediaGallery.PickAsync(request);
            var files = results?.Files?.ToArray();
            if (files == null || files.Length == 0) return;

            try
            {
                App.MainWindow.Page.IsEnabled = false;
                App.MainWindow.Page.IsBusy = true;

                using var file = files[0];
                using var stream = await file.OpenReadAsync();
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);

                var fileName = file.GenerateFileName();
                var bytes = memoryStream.ToArray();
                try
                {
                    await Shared.ApiHandler.ExecuteRequestAsync(new UpdateBackgroundMedia(fileName, bytes));
                    await RefreshAsync();
                }
                catch (HttpRequestException exception)
                {
                    await App.MainWindow.Page.DisplayAlert("배경 이미지 변경 실패", $"알 수 없는 오류가 발생했습니다\n코드: {exception.Message}", "확인");
                }
            }
            finally
            {
                App.MainWindow.Page.IsEnabled = true;
                App.MainWindow.Page.IsBusy = false;
            }

        }
    }

    public async Task HandleChangeProfileMediaAsync()
    {
        bool shouldUpload = true;
        if (User.ProfileMediaId != null)
        {
            var result = await App.MainWindow.Page.DisplayActionSheet("프로필 이미지", "취소", null, ["프로필 이미지 변경", "프로필 이미지 삭제"]);
            if (result == "취소") return;
            else if (result == "프로필 이미지 변경") shouldUpload = true;
            else if (result == "프로필 이미지 삭제")
            {
                try
                {
                    App.MainWindow.Page.IsEnabled = false;
                    App.MainWindow.Page.IsBusy = true;

                    await Shared.ApiHandler.ExecuteRequestAsync(new DeleteProfileMedia());
                    await RefreshAsync();
                    return;
                }
                finally
                {
                    App.MainWindow.Page.IsEnabled = true;
                    App.MainWindow.Page.IsBusy = false;
                }
            }
        }

        if (shouldUpload)
        {
            var request = new MediaPickRequest(1, MediaFileType.Image, MediaFileType.Video)
            {
                Title = "프로필 이미지 선택"
            };

            var results = await MediaGallery.PickAsync(request);
            var files = results?.Files?.ToArray();
            if (files == null || files.Length == 0) return;

            try
            {
                App.MainWindow.Page.IsEnabled = false;
                App.MainWindow.Page.IsBusy = true;

                using var file = files[0];
                using var stream = await file.OpenReadAsync();
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);

                var fileName = file.GenerateFileName();
                var bytes = memoryStream.ToArray();
                try
                {
                    await Shared.ApiHandler.ExecuteRequestAsync(new UpdateProfileMedia(fileName, bytes));
                    await RefreshAsync();
                }
                catch (HttpRequestException exception)
                {
                    await App.MainWindow.Page.DisplayAlert("프로필 이미지 변경 실패", $"알 수 없는 오류가 발생했습니다\n코드: {exception.Message}", "확인");
                }
            }
            finally
            {
                App.MainWindow.Page.IsEnabled = true;
                App.MainWindow.Page.IsBusy = false;
            }
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
