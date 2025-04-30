using CommunityToolkit.Mvvm.ComponentModel;
using History.Commons;
using History.Commons.Api.Friendship;
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
    public partial UserResponseDto User { get; private set; } = user;

    public bool IsMe => User.UserId == Shared.UserId;
    public bool IsNotMe => User.UserId != Shared.UserId;

    public string FriendButtonText
    {
        get
        {
            if (IsMe) return "ERROR";
            else if (User.Friendship != null && User.Friendship.Status == FriendshipStatus.Accepted) return "친구 삭제";
            else if (User.Friendship != null && User.Friendship.Status == FriendshipStatus.Waiting) return "친구 수락 / 거절";
            else if (User.Friendship != null && User.Friendship.Status == FriendshipStatus.Requested) return "친구 요청 취소";
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
                    return $"{friendDays:N0}일째 친구에요!";
                else if (friendDays < 365)
                    return $"{friendDays / 30:N0}개월째 친구에요!";
                else
                    return $"{friendDays / 365:N0}년째 친구에요!";
            }
            else if (User.Friendship != null && User.Friendship.Status == FriendshipStatus.Requested)
            {
                return "친구 요청을 보냈어요!";
            }
            else return "친구가 아니에요.";
        }
    }

    public IMediaViewModel BackgroundMedia => User.UsesAnimatedBackgroundMedia
        ? new VideoViewModel(Utils.GenerateMediaUri(User.BackgroundMediaId))
        : new ImageViewModel(Utils.GenerateMediaUri(User.BackgroundMediaId) ?? Constants.DefaultBackgroundImageFileName);

    public IMediaViewModel ProfileMedia => User.UsesAnimatedProfileMedia
        ? new VideoViewModel(Utils.GenerateMediaUri(User.ProfileMediaId))
        : new ImageViewModel(Utils.GenerateMediaUri(User.ProfileMediaId) ?? Constants.DefaultProfileImageFileName);

    private async Task RefreshAsync()
    {
        var result = await App.ExecuteRequestAsync(new GetUser(User.UserId));
        if (result.IsSuccess) User = result.Value;
    }

    public async Task HandleChangeBackgroundMediaAsync()
    {
        bool shouldUpload = true;
        if (User.BackgroundMediaId != null)
        {
            var action = await App.MainWindow.Page.DisplayActionSheet("배경 이미지", Constants.PromptCancel, null, ["배경 이미지 변경", "배경 이미지 삭제"]);
            if (action == Constants.PromptCancel) return;
            else if (action == "배경 이미지 변경") shouldUpload = true;
            else if (action == "배경 이미지 삭제")
            {
                var result = await App.ExecuteRequestAsync(new DeleteBackgroundMedia());
                if (result.IsSuccess) await RefreshAsync();
                return;
            }
        }

        if (shouldUpload)
        {
            var request = new MediaPickRequest(1, MediaFileType.Image) { Title = "배경 이미지 선택" };

            var results = await MediaGallery.PickAsync(request);
            var files = results?.Files?.ToArray();
            if (files == null || files.Length == 0) return;

            using var file = files[0];
            using var stream = await file.OpenReadAsync();
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);

            var fileName = file.GenerateFileName();
            var bytes = memoryStream.ToArray();

            var result = await App.ExecuteRequestAsync(new UpdateBackgroundMedia(fileName, bytes));
            if(result.IsSuccess) await RefreshAsync();
        }
    }

    public async Task HandleChangeProfileMediaAsync()
    {
        bool shouldUpload = true;
        if (User.ProfileMediaId != null)
        {
            var action = await App.MainWindow.Page.DisplayActionSheet("프로필 이미지", Constants.PromptCancel, null, ["프로필 이미지 변경", "프로필 이미지 삭제"]);
            if (action == Constants.PromptCancel) return;
            else if (action == "프로필 이미지 변경") shouldUpload = true;
            else if (action == "프로필 이미지 삭제")
            {
                var result = await App.ExecuteRequestAsync(new DeleteProfileMedia());
                if(result.IsSuccess) await RefreshAsync();
                return;
            }
        }

        if (shouldUpload)
        {
            var request = new MediaPickRequest(1, MediaFileType.Image)
            {
                Title = "프로필 이미지 선택"
            };

            var results = await MediaGallery.PickAsync(request);
            var files = results?.Files?.ToArray();
            if (files == null || files.Length == 0) return;

            using var file = files[0];
            using var stream = await file.OpenReadAsync();
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);

            var fileName = file.GenerateFileName();
            var bytes = memoryStream.ToArray();

            var result = await App.ExecuteRequestAsync(new UpdateProfileMedia(fileName, bytes));
            if (result.IsSuccess) await RefreshAsync();
        }
    }

    public async Task HandleFriendshipActionAsync()
    {
        if (User.Friendship == null)
        {
            var result = await App.MainWindow.Page.DisplayAlert("안내", $"{Nickname}에게 친구 신청을 보내시겠습니까?", Constants.PromptYes, Constants.PromptNo);
            if (result) await App.ExecuteRequestAsync(new SendFriendRequest(User.UserId));
        }
        else if (User.Friendship.Status == FriendshipStatus.Accepted)
        {
            var result = await App.MainWindow.Page.DisplayAlert("안내", $"{Nickname}와의 친구 관계를 끊으시겠습니까?", Constants.PromptYes, Constants.PromptNo);
            if (result) await App.ExecuteRequestAsync(new RemoveFriend(User.UserId));
        }
        else if (User.Friendship.Status == FriendshipStatus.Requested)
        {
            var result = await App.MainWindow.Page.DisplayAlert("안내", $"{Nickname}에게 보낸 친구 신청을 취소하시겠습니까? 상대방에게 이미 보낸 친구 신청 알림은 취소되지 않습니다.", Constants.PromptYes, Constants.PromptNo);
            if (result) await App.ExecuteRequestAsync(new CancelFriendRequest(User.UserId));
        }
        else if (User.Friendship.Status == FriendshipStatus.Waiting)
        {
            var result = await App.MainWindow.Page.DisplayAlert("안내", $"{Nickname}의 친구 신청을 수락하시겠습니까?", Constants.PromptYes, Constants.PromptNo);
            if (result) await App.ExecuteRequestAsync(new AcceptFriendRequest(User.UserId));
        }

        await RefreshAsync();
    }

    public async void OnEditNicknameImageTapped(object sender, TappedEventArgs e)
    {
        var prompt = await App.MainWindow.Page.DisplayPromptAsync("닉네임 변경", "새로운 닉네임을 입력해주세요", "변경", Constants.PromptCancel, "새로운 닉네임", 40, Keyboard.Plain, User.Nickname);
        prompt = prompt?.Trim();

        if (prompt != null && prompt != User.Nickname)
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                await App.MainWindow.Page.DisplayAlert("닉네임 변경 실패", "닉네임은 공백으로 설정할 수 없습니다", Constants.PromptOk);
                return;
            }
            else if (prompt.Length > CommonsConstants.MaxNicknameLength)
            {
                await App.MainWindow.Page.DisplayAlert("닉네임 변경 실패", $"닉네임은 {CommonsConstants.MaxNicknameLength}자 이하로 설정할 수 있습니다", Constants.PromptOk);
                return;
            }

            var result = await App.ExecuteRequestAsync(new UpdateNickname(prompt));
            if (result.IsSuccess) await RefreshAsync();
        }
    }

    public async void OnEditDescriptionImageTapped(object sender, TappedEventArgs e)
    {
        var prompt = await App.MainWindow.Page.DisplayPromptAsync("한줄 소개 변경", "새로운 한줄 소개를 입력해주세요 (공백 시 설정 해제)", "변경", Constants.PromptCancel, "새로운 한줄 소개 (공백 시 설정 해제)", 40, Keyboard.Plain, User.Description);
        prompt = prompt?.Trim();

        if (prompt != null && prompt != User.Description)
        {
            if (prompt.Length > CommonsConstants.MaxProfileDescriptionLength)
            {
                await App.MainWindow.Page.DisplayAlert("한줄 소개 변경 실패", $"한줄 소개는 {CommonsConstants.MaxProfileDescriptionLength}자 이하로 설정할 수 있습니다", Constants.PromptOk);
                return;
            }

            var result = await App.ExecuteRequestAsync(new UpdateDescription(prompt));
            if (result.IsSuccess) await RefreshAsync();
        }
    }
}
