using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using FFImageLoading.Maui;
using History.Commons;
using History.Commons.Api.Friendship;
using History.Commons.Api.User;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using History.MobileClient.Helpers;
using NativeMedia;

namespace History.MobileClient.ViewModels;

public partial class ProfileViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMe))]
    [NotifyPropertyChangedFor(nameof(IsNotMe))]
    [NotifyPropertyChangedFor(nameof(IsFriend))]
    [NotifyPropertyChangedFor(nameof(FriendButtonText))]
    [NotifyPropertyChangedFor(nameof(Nickname))]
    [NotifyPropertyChangedFor(nameof(Description))]
    [NotifyPropertyChangedFor(nameof(FriendshipDescription))]
    [NotifyPropertyChangedFor(nameof(BackgroundMedia))]
    [NotifyPropertyChangedFor(nameof(ProfileMedia))]
    public partial UserResponseDto User { get; private set; }

    public bool IsMe => User.UserId == Shared.UserId;
    public bool IsNotMe => !IsMe;
    public bool IsFriend => User.Friendship?.Status == FriendshipStatus.Accepted;

    public bool IsModerator => User.Rank == Rank.Moderator;
    public bool IsAdmin => User.Rank == Rank.Admin;

    public string FriendButtonText
    {
        get
        {
            if (IsMe) return "ERROR";
            else if (User.Friendship != null && User.Friendship.Status == FriendshipStatus.Accepted) return "친구 삭제 / 차단 / 무시";
            else if (User.Friendship != null && User.Friendship.Status == FriendshipStatus.Waiting) return "친구 수락 / 거절";
            else if (User.Friendship != null && User.Friendship.Status == FriendshipStatus.Requested) return "친구 요청 취소";
            else return "친구 신청 / 차단 / 무시";
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

    public IMediaViewModel BackgroundMedia => new ImageViewModel(Utils.GenerateMediaUri(User.BackgroundThumbnailMediaId) ?? Constants.DefaultBackgroundImageFileName);

    public IMediaViewModel ProfileMedia => User.UsesAnimatedProfileMedia
        ? new VideoViewModel(Utils.GenerateMediaUri(User.ProfileMediaId))
        : new ImageViewModel(Utils.GenerateMediaUri(User.ProfileMediaId) ?? Constants.DefaultProfileImageFileName);

    public ProfileViewModel(UserResponseDto user)
    {
        User = user;
        WeakReferenceMessenger.Default.Register<ValueChangedMessage<UserResponseDto>>(this, (r, m) =>
        {
            if (m.Value.UserId != User.UserId) return;

            User = m.Value;
        });
    }

    private async Task RefreshAsync()
    {
        var result = await App.ExecuteRequestAsync(new GetUser(User.UserId));
        if (result.IsSuccess) WeakReferenceMessenger.Default.Send(new ValueChangedMessage<UserResponseDto>(result.Value));
    }

    private async Task HandleChangeNicknameAsync()
    {
        var prompt = await App.Page.DisplayPromptAsync("닉네임 변경", "새로운 닉네임을 입력해주세요", "변경", Constants.PromptCancel, "새로운 닉네임", 40, Keyboard.Plain, User.Nickname);
        prompt = prompt?.Trim();

        if (prompt != null && prompt != User.Nickname)
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                await App.Page.DisplayAlert("닉네임 변경 실패", "닉네임은 공백으로 설정할 수 없습니다", Constants.PromptOk);
                return;
            }
            else if (prompt.Length > CommonsConstants.MaxNicknameLength)
            {
                await App.Page.DisplayAlert("닉네임 변경 실패", $"닉네임은 {CommonsConstants.MaxNicknameLength}자 이하로 설정할 수 있습니다", Constants.PromptOk);
                return;
            }

            var result = await App.ExecuteRequestAsync(new UpdateNickname(prompt));
            if (result.IsSuccess) await RefreshAsync();
        }
    }

    private async Task HandleChangeDescriptionAsync()
    {
        var prompt = await App.Page.DisplayPromptAsync("한줄 소개 변경", "새로운 한줄 소개를 입력해주세요 (공백 시 설정 해제)", "변경", Constants.PromptCancel, "새로운 한줄 소개 (공백 시 설정 해제)", 40, Keyboard.Plain, User.Description);
        prompt = prompt?.Trim();

        if (prompt != null && prompt != User.Description)
        {
            if (prompt.Length > CommonsConstants.MaxProfileDescriptionLength)
            {
                await App.Page.DisplayAlert("한줄 소개 변경 실패", $"한줄 소개는 {CommonsConstants.MaxProfileDescriptionLength}자 이하로 설정할 수 있습니다", Constants.PromptOk);
                return;
            }

            var result = await App.ExecuteRequestAsync(new UpdateDescription(prompt));
            if (result.IsSuccess) await RefreshAsync();
        }
    }

    private async Task HandleChangeProfileMediaAsync()
    {
        bool shouldUpload = true;
        if (User.ProfileMediaId != null)
        {
            var action = await App.Page.DisplayActionSheet("프로필 이미지", Constants.PromptCancel, null, ["프로필 이미지 변경", "프로필 이미지 삭제"]);
            if (action == Constants.PromptCancel) return;
            else if (action == "프로필 이미지 변경") shouldUpload = true;
            else if (action == "프로필 이미지 삭제")
            {
                var result = await App.ExecuteRequestAsync(new DeleteProfileMedia());
                if (result.IsSuccess) await RefreshAsync();
                return;
            }
        }

        if (shouldUpload)
        {
            string fileName;
            byte[] bytes;

#if IOS
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

            fileName = file.GenerateFileName();
            bytes = memoryStream.ToArray();
#elif ANDROID
            var image = await AndroidMediaPickerHelper.PickMediaAsync(true, false);
            if (image == null) return;

            fileName = image.FileName;
            bytes = image.Bytes;
#endif
            var result = await App.ExecuteRequestAsync(new UpdateProfileMedia(fileName, bytes));
            if (result.IsSuccess) await RefreshAsync();
        }
    }

    private async Task HandleChangeBackgroundMediaAsync()
    {
        bool shouldUpload = true;
        if (User.BackgroundMediaId != null)
        {
            var action = await App.Page.DisplayActionSheet("배경 이미지", Constants.PromptCancel, null, ["배경 이미지 변경", "배경 이미지 삭제"]);
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
            string fileName;
            byte[] bytes;

#if IOS
            var request = new MediaPickRequest(1, MediaFileType.Image) { Title = "배경 이미지 선택" };

            var results = await MediaGallery.PickAsync(request);
            var files = results?.Files?.ToArray();
            if (files == null || files.Length == 0) return;

            using var file = files[0];
            using var stream = await file.OpenReadAsync();
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);

            fileName = file.GenerateFileName();
            bytes = memoryStream.ToArray();
#elif ANDROID
            var media = await AndroidMediaPickerHelper.PickMediaAsync(true, true);
            if (media == null) return;

            fileName = media.FileName;
            bytes = media.Bytes;
#endif

            var result = await App.ExecuteRequestAsync(new UpdateBackgroundMedia(fileName, bytes));
            if (result.IsSuccess) await RefreshAsync();
        }
    }

    private async Task HandleChangeHandleAsync()
    {
        var handle = await App.Page.DisplayPromptAsync("핸들 변경", "새로운 핸들을 입력해주세요 (최대 20자, 특수문자 사용 불가)", "변경", Constants.PromptCancel, "새로운 핸들", CommonsConstants.MaxHandleLength, null, User.Handle);
        handle = handle?.Trim();
        if (handle != null)
        {
            var result = await App.ExecuteRequestAsync(new UpdateHandle(handle), [ErrorType.BadRequest, ErrorType.Conflict]);
            if (result.IsSuccess)
            {
                await App.Page.DisplayAlert("안내", "핸들이 변경되었습니다.", Constants.PromptOk);
                await RefreshAsync();
            }
            else if (result.Error == ErrorType.BadRequest || result.Error == ErrorType.Conflict) await App.Page.DisplayAlert("핸들 변경 실패", result.ErrorMessage, Constants.PromptOk);
        }
    }

    private async Task HandleChangeProfileVisibilityAsync()
    {
        var action = await App.Page.DisplayActionSheet("프로필 공개 설정", Constants.PromptCancel, null, "공개", "비공개");
        if (action == null || action == Constants.PromptCancel) return;

        var allowSearch = action == "공개";
        var result = await App.ExecuteRequestAsync(new UpdateAllowSearch(allowSearch));
        if (result.IsSuccess)
        {
            if (allowSearch) await App.Page.DisplayAlert("안내", "프로필 공개 설정이 완료되었습니다. 이제부터 다른 사용자가 닉네임이나 핸들을 통해 내 프로필을 검색할 수 있습니다.", Constants.PromptOk);
            else await App.Page.DisplayAlert("안내", "프로필 비공개 설정이 완료되었습니다. 이제부터 다른 사용자가 닉네임이나 핸들을 통해 내 프로필을 검색할 수 없습니다.", Constants.PromptOk);
            await RefreshAsync();
        }
        else await App.Page.DisplayAlert("오류", result.ErrorMessage, Constants.PromptOk);
    }

    private static async Task HandleChangeFriendListDiscoveryOptionAsync()
    {
        var discoveryOptions = Enum.GetValues<DiscoveryOption>().ToList();
        discoveryOptions.Remove(DiscoveryOption.SelectedUsers);
        discoveryOptions.Remove(DiscoveryOption.UnselectedUsers);

        var rawDiscoveryOptions = discoveryOptions.Select(x => x.ToDisplayString()).ToArray();
        var rawDiscoveryOption = await App.Page.DisplayActionSheet("친구 목록 공개 범위 설정", Constants.PromptCancel, null, rawDiscoveryOptions);

        if (rawDiscoveryOption == null || rawDiscoveryOption == Constants.PromptCancel) return;

        var discoveryOption = DiscoveryOptionExtensions.FromDisplayString(rawDiscoveryOption);
        var result = await App.ExecuteRequestAsync(new UpdateFriendListDiscoveryOption(discoveryOption));
        if (result.IsSuccess) await App.Page.DisplayAlert("안내", $"친구 목록 공개 범위 설정이 {rawDiscoveryOption} 으로 변경되었습니다.", Constants.PromptOk);
    }

    [RelayCommand]
    private async Task HandleFriendshipActionAsync()
    {
        Result result = null;

        async Task Block()
        {
            var block = await App.Page.DisplayAlert("안내", $"정말로 {Nickname}님을 차단하시겠습니까? 차단하는 경우, 해제할 때 까지  히스토리에서 나와 상대방 모두 서로를 볼 수 없게 됩니다.", Constants.PromptYes, Constants.PromptNo);
            if (block) result = await App.ExecuteRequestAsync(new BlockUser(User.UserId));
            await App.PopModalAsync();
            return;
        }

        async Task Ignore()
        {
            var block = await App.Page.DisplayAlert("안내", $"정말로 {Nickname}님을 무시하시겠습니까? 무시하는 경우, 해제할 때 까지 히스토리에서 상대방을 볼 수 없습니다. 다만, 상대방은 나를 볼 수 있습니다.", Constants.PromptYes, Constants.PromptNo);
            if (block) result = await App.ExecuteRequestAsync(new IgnoreUser(User.UserId));
            await App.PopModalAsync();
            return;
        }

        if (User.Friendship == null)
        {
            var action = await App.Page.DisplayActionSheet("친구 신청 / 차단 / 무시", Constants.PromptCancel, null, ["친구 신청", "차단", "무시"]);
            if (action == null || action == Constants.PromptCancel) return;

            if (action == "친구 신청")
            {
                var send = await App.Page.DisplayAlert("안내", $"{Nickname}님에게 친구 신청을 보내시겠습니까?", Constants.PromptYes, Constants.PromptNo);
                if (send) result = await App.ExecuteRequestAsync(new SendFriendRequest(User.UserId));
            }
            else if (action == "차단") await Block();
            else if (action == "무시") await Ignore();
        }
        else if (User.Friendship.Status == FriendshipStatus.Accepted)
        {
            var action = await App.Page.DisplayActionSheet("친구 삭제 / 차단 / 무시", Constants.PromptCancel, null, ["친구 삭제", "차단", "무시"]);
            if (action == null || action == Constants.PromptCancel) return;

            if (action == "친구 삭제")
            {
                var delete = await App.Page.DisplayAlert("안내", $"{Nickname}님와의 친구 관계를 끊으시겠습니까?", Constants.PromptYes, Constants.PromptNo);
                if (delete) result = await App.ExecuteRequestAsync(new RemoveFriend(User.UserId));
            }
            else if (action == "차단") await Block();
            else if (action == "무시") await Ignore();
        }
        else if (User.Friendship.Status == FriendshipStatus.Requested)
        {
            var cancel = await App.Page.DisplayAlert("안내", $"{Nickname}님에게 보낸 친구 신청을 취소하시겠습니까? 상대방에게 이미 보낸 친구 신청 알림은 취소되지 않습니다.", Constants.PromptYes, Constants.PromptNo);
            if (cancel) result = await App.ExecuteRequestAsync(new CancelFriendRequest(User.UserId));
        }
        else if (User.Friendship.Status == FriendshipStatus.Waiting)
        {
            var accept = await App.Page.DisplayAlert("안내", $"{Nickname}님의 친구 신청을 수락하시겠습니까?", Constants.PromptYes, Constants.PromptNo);
            if (accept) result = await App.ExecuteRequestAsync(new AcceptFriendRequest(User.UserId));
        }

        if (result != null && result.IsSuccess) await RefreshAsync();
    }

    [RelayCommand]
    private async Task HandleProfileSettingsAsync()
    {
        var action = await App.Page.DisplayActionSheet("프로필 설정", Constants.PromptCancel, null, "닉네임 변경", "한줄 소개 변경", "프로필 이미지 설정", "배경 이미지 설정", "핸들 변경", "프로필 공개 설정", "친구 목록 공개 범위 설정");

        if (action == null || action == Constants.PromptCancel) return;

        if (action == "닉네임 변경") await HandleChangeNicknameAsync();
        else if (action == "한줄 소개 변경") await HandleChangeDescriptionAsync();
        else if (action == "프로필 이미지 설정") await HandleChangeProfileMediaAsync();
        else if (action == "배경 이미지 설정") await HandleChangeBackgroundMediaAsync();
        else if (action == "핸들 변경") await HandleChangeHandleAsync();
        else if (action == "프로필 공개 설정") await HandleChangeProfileVisibilityAsync();
        else if (action == "친구 목록 공개 범위 설정") await HandleChangeFriendListDiscoveryOptionAsync();
    }
}
