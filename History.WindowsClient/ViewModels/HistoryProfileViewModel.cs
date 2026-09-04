using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using History.Commons;
using History.Commons.Api.Friendship;
using History.Commons.Api.User;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using History.WindowsClient.Helpers;
using History.WindowsClient.Messages;
using History.WindowsClient.Pages;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.ApplicationModel.DataTransfer;

namespace History.WindowsClient.ViewModels;

// Owns the user DTO, fills the shared BaseProfileViewModel surface, and
// implements the friendship/favorite/ban/memo/copy actions with the
// WindowsClient dialog conventions (see HistoryFriendshipViewModel).
public partial class HistoryProfileViewModel : BaseProfileViewModel, IRecipient<ValueChangedMessage<UserResponseDto>>
{
    [ObservableProperty]
    public partial UserResponseDto User { get; private set; }

    private readonly string _userId;
    private readonly BaseViewModel _baseViewModel;
    private bool _isCopyProfileLinkFeedbackActive;

    public HistoryProfileViewModel(UserResponseDto user, BaseViewModel baseViewModel)
    {
        _userId = user.UserId;
        _baseViewModel = baseViewModel;

        Update(user);

        WeakReferenceMessenger.Default.Register(this);
    }

    public void Receive(ValueChangedMessage<UserResponseDto> message)
    {
        if (message.Value.UserId != _userId) return;

        Update(message.Value);
    }

    public async Task RefreshAsync()
    {
        var result = await _baseViewModel.ExecuteRequestAsync(new GetUser(_userId));
        if (!result.IsSuccess)
        {
            await _baseViewModel.ShowMessageDialogAsync(new("오류", "프로필 정보 갱신에 실패하였습니다."));
            return;
        }

        WeakReferenceMessenger.Default.Send(new ValueChangedMessage<UserResponseDto>(result.Value));
    }

    // Friendship action flow handled with the HistoryFriendshipViewModel dialog conventions.
    public override async Task HandleFriendshipActionAsync()
    {
        Result result = null;
        FriendshipStatus? newStatus = null;

        if (User.Friendship == null)
        {
            var dialogResult = await _baseViewModel.ShowMessageDialogAsync(new("안내", $"{Nickname}님에게 친구 신청을 보내시겠습니까?", DialogHelper.DefaultOkButtonText, DialogHelper.DefaultCancelButtonText));
            if (dialogResult == ContentDialogResult.Primary)
            {
                result = await _baseViewModel.ExecuteRequestAsync(new SendFriendRequest(User.UserId));
                newStatus = FriendshipStatus.Requested;
            }
        }
        else if (User.Friendship.Status == FriendshipStatus.Accepted)
        {
            var dialogResult = await _baseViewModel.ShowMessageDialogAsync(new("안내", $"{Nickname}님과의 친구 관계를 끊으시겠습니까?", DialogHelper.DefaultOkButtonText, DialogHelper.DefaultCancelButtonText));
            if (dialogResult == ContentDialogResult.Primary)
            {
                result = await _baseViewModel.ExecuteRequestAsync(new RemoveFriend(User.UserId));
                newStatus = null;
            }
        }
        else if (User.Friendship.Status == FriendshipStatus.Requested)
        {
            var dialogResult = await _baseViewModel.ShowMessageDialogAsync(new("안내", $"{Nickname}님에게 보낸 친구 신청을 취소하시겠습니까? 상대방에게 이미 보낸 친구 신청 알림은 취소되지 않습니다.", DialogHelper.DefaultOkButtonText, DialogHelper.DefaultCancelButtonText));
            if (dialogResult == ContentDialogResult.Primary)
            {
                result = await _baseViewModel.ExecuteRequestAsync(new CancelFriendRequest(User.UserId));
                newStatus = null;
            }
        }
        else if (User.Friendship.Status == FriendshipStatus.Waiting)
        {
            var dialogResult = await _baseViewModel.ShowMessageDialogAsync(new("안내", $"{Nickname}님의 친구 신청을 수락하시겠습니까?", DialogHelper.DefaultOkButtonText, DialogHelper.DefaultCancelButtonText));
            if (dialogResult == ContentDialogResult.Primary)
            {
                result = await _baseViewModel.ExecuteRequestAsync(new AcceptFriendRequest(User.UserId));
                newStatus = FriendshipStatus.Accepted;
            }
        }

        if (result != null && result.IsSuccess)
        {
            await RefreshAsync();
            WeakReferenceMessenger.Default.Send(new FriendshipChangedMessage(User.UserId, newStatus, User));
        }
    }

    public override async Task HandleFavoriteAsync()
    {
        var result = await _baseViewModel.ExecuteRequestAsync(new ToggleFavorite(User.UserId));
        if (result.IsSuccess) await RefreshAsync();
    }

    // An empty memo deletes it, and success refreshes the profile surface.
    public override async Task HandleMemoAsync()
    {
        var memo = await _baseViewModel.ShowInputDialogAsync(new("메모 작성", "사용자 메모를 작성해주세요. 공란으로 설정 시 메모가 삭제됩니다.", placeholderText: $"최대 {CommonConstants.MaxMemoLength}자까지 입력 가능. 공란 시 삭제", showCancel: true, maxLength: CommonConstants.MaxMemoLength));
        if (memo == null) return;

        var result = await _baseViewModel.ExecuteRequestAsync(new UpdateMemo(User.UserId, memo));
        if (result.IsSuccess) await RefreshAsync();
    }

    // Shows the checkmark glyph on the button for two seconds after copying and
    // ignores re-taps while that feedback is active.
    public override async Task HandleCopyProfileLinkAsync()
    {
        if (_isCopyProfileLinkFeedbackActive) return;

        var dataPackage = new DataPackage { RequestedOperation = DataPackageOperation.Copy };
        dataPackage.SetText($"https://historyweb.cc/u/{User.UserId}");
        Clipboard.SetContent(dataPackage);

        _isCopyProfileLinkFeedbackActive = true;
        CopyProfileLinkGlyph = CheckMarkGlyph;
        await Task.Delay(2000);
        CopyProfileLinkGlyph = LinkGlyph;
        _isCopyProfileLinkFeedbackActive = false;
    }

    public override async Task HandleBanAsync()
    {
        var action = await _baseViewModel.ShowSelectionDialogAsync("사용자 차단 / 무시", ["차단", "무시"]);
        if (action == "차단") await BlockAsync();
        else if (action == "무시") await IgnoreAsync();
    }

    private async Task BlockAsync()
    {
        var block = await _baseViewModel.ShowMessageDialogAsync(new("안내", $"정말로 {Nickname}님을 차단하시겠습니까? 차단하는 경우, 해제할 때까지 히스토리에서 나와 상대방 모두 서로를 볼 수 없게 됩니다. 또한, 친구 관계인 경우 친구 삭제가 먼저 선행됩니다.", DialogHelper.DefaultOkButtonText, DialogHelper.DefaultCancelButtonText));
        if (block != ContentDialogResult.Primary) return;

        var result = await _baseViewModel.ExecuteRequestAsync(new BlockUser(User.UserId));
        if (result.IsFailure) return;

        WeakReferenceMessenger.Default.Send(new FriendshipChangedMessage(User.UserId, FriendshipStatus.Blocked, User));
        await _baseViewModel.TryNavigateBackAsync();
    }

    private async Task IgnoreAsync()
    {
        var block = await _baseViewModel.ShowMessageDialogAsync(new("안내", $"정말로 {Nickname}님을 무시하시겠습니까? 무시하는 경우, 해제할 때까지 히스토리에서 상대방을 볼 수 없습니다. 다만, 상대방은 나를 볼 수 있습니다. 또한, 친구 관계인 경우 친구 삭제가 먼저 선행됩니다.", DialogHelper.DefaultOkButtonText, DialogHelper.DefaultCancelButtonText));
        if (block != ContentDialogResult.Primary) return;

        var result = await _baseViewModel.ExecuteRequestAsync(new IgnoreUser(User.UserId));
        if (result.IsFailure) return;

        WeakReferenceMessenger.Default.Send(new FriendshipChangedMessage(User.UserId, FriendshipStatus.Ignored, User));
        await _baseViewModel.TryNavigateBackAsync();
    }

    public override void HandleProfileTap(string parameter)
    {
        if (parameter == "Navigate") _baseViewModel.RequestNavigation(typeof(ProfilePage), User.UserId);
        else
        {
            // TODO: Open the profile image in the full-screen media viewer once it is implemented.
        }
    }

    // TODO: Open the background image in the full-screen media viewer once it is implemented.
    public override void HandleBackgroundTap() { }

    private void Update(UserResponseDto user)
    {
        // Compute all derived properties from the new user before assigning User.
        IsMe = user.UserId == CommonShared.UserId;
        IsFriend = user.Friendship?.Status == FriendshipStatus.Accepted;
        IsModerator = user.Rank == Rank.Moderator;
        IsAdmin = user.Rank == Rank.Admin;
        IsFavorite = user.IsFavorite;

        Nickname = user.Nickname;
        Description = string.IsNullOrEmpty(user.Description) ? "설정된 한줄 소개가 없습니다" : user.Description;
        FriendButtonText = GetFriendButtonText(user);
        FriendshipDescription = GetFriendshipDescription(user);
        FavoriteBrush = IsFavorite ? CreateFavoriteBrush() : CreateNotFavoriteBrush();

        ProfileThumbnailImageSource = user.ProfileThumbnailMediaId != null ? new BitmapImage(new Uri(CommonUtils.GenerateMediaUri(user.ProfileThumbnailMediaId))) : null;
        ProfileImageSource = user.ProfileMediaId != null ? new BitmapImage(new Uri(CommonUtils.GenerateMediaUri(user.ProfileMediaId))) : null;
        BackgroundImageSource = user.BackgroundMediaId != null ? new BitmapImage(new Uri(CommonUtils.GenerateMediaUri(user.BackgroundMediaId))) : null;

        // Assign User last so all derived properties are already up-to-date.
        User = user;
    }

    private static string GetFriendButtonText(UserResponseDto user)
    {
        if (user.UserId == CommonShared.UserId) return "ERROR";
        else if (user.Friendship != null && user.Friendship.Status == FriendshipStatus.Accepted) return "친구 삭제";
        else if (user.Friendship != null && user.Friendship.Status == FriendshipStatus.Waiting) return "친구 수락";
        else if (user.Friendship != null && user.Friendship.Status == FriendshipStatus.Requested) return "친구 요청 취소";
        else return "친구 신청";
    }

    private static string GetFriendshipDescription(UserResponseDto user)
    {
        if (user.UserId == CommonShared.UserId) return "내 프로필입니다.";
        else if (user.Friendship != null && user.Friendship.Status == FriendshipStatus.Accepted)
        {
            var friendDays = (DateTime.UtcNow - user.Friendship.CreatedAt).TotalDays;
            if (friendDays < 1) return "친구가 된지 하루도 안됐어요!";
            else if (friendDays < 30) return $"{friendDays:N0}일째 친구에요!";
            else if (friendDays < 365) return $"{friendDays / 30:N0}개월째 친구에요!";
            else return $"{friendDays / 365:N0}년째 친구에요!";
        }
        else if (user.Friendship != null && user.Friendship.Status == FriendshipStatus.Requested) return "친구 요청을 보냈어요!";
        else return "친구가 아니에요.";
    }

    private static Brush CreateFavoriteBrush() => new SolidColorBrush((Windows.UI.Color)Application.Current.Resources["SystemAccentColor"]);

    private static Brush CreateNotFavoriteBrush() => new SolidColorBrush(Windows.UI.Color.FromArgb(0xFF, 0x30, 0x30, 0x30));
}
