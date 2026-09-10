using CommunityToolkit.Mvvm.ComponentModel;
using History.Commons;
using History.Commons.Api.User;
using History.Commons.KakaoStory;
using History.WindowsClient.Helpers;
using History.WindowsClient.Models;
using History.WindowsClient.Pages;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI;
using static History.Commons.KakaoStory.KakaoStoryApiHandler.DataType;

namespace History.WindowsClient.ViewModels;

// Kakao Story profile view model: fills the shared profile surface from the profile
// feed response (ProfileData.Profile + MutualFriend) and implements the friendship,
// favorite, ban, copy-link and profile settings actions.
public partial class KakaoProfileViewModel : BaseProfileViewModel
{
    private const string DefaultBackgroundImageUrlPrefix = "https://t1.kakaocdn.net/story_static/public/images/wallpapers/";

    private readonly BaseViewModel _baseViewModel;
    private bool _isCopyProfileLinkFeedbackActive;

    [ObservableProperty]
    public partial ProfileData.Profile Profile { get; private set; }

    public ProfileData.MutualFriend MutualFriend { get; private set; }

    public string KakaoUserId => Profile?.id;

    // A profile image is considered missing while the default image is in use.
    public bool HasProfileImage => Profile != null && Profile.is_default_profile_image != true;

    // A background image is considered missing while the default wallpaper is in use.
    public bool HasBackgroundImage => Profile?.bg_image_url != null && !Profile.bg_image_url.StartsWith(DefaultBackgroundImageUrlPrefix, StringComparison.Ordinal);

    // A user banned by me (차단), distinct from Profile.blocked (Kakao-suspended user).
    public bool IsBanned => Profile?.relation?.ban == "A";

    public KakaoProfileViewModel(ProfileData.Profile profile, ProfileData.MutualFriend mutualFriend, BaseViewModel baseViewModel)
    {
        _baseViewModel = baseViewModel;
        Profile = profile;
        MutualFriend = mutualFriend;
        UpdateSurface();
    }

    private void UpdateSurface()
    {
        IsMe = Profile?.id == CommonShared.KakaoUserId;
        IsFriend = Profile?.relationship == "F";
        IsModerator = false;
        IsAdmin = false;
        IsFavorite = Profile?.is_favorite ?? false;
        IsBlocked = Profile?.blocked ?? false;
        IsMemoVisible = false;
        BanButtonText = "차단";
        FavoriteBrush = IsFavorite ? new SolidColorBrush((Color)Application.Current.Resources["SystemAccentColor"]) : new SolidColorBrush(Color.FromArgb(0xFF, 0x30, 0x30, 0x30));

        Nickname = Profile?.display_name;
        Description = Profile?.status_objects?.FirstOrDefault()?.message ?? "설정된 한줄 소개가 없습니다";
        FriendButtonText = GetFriendButtonText();
        FriendshipDescription = IsMe ? "내 프로필입니다." : (MutualFriend?.message ?? "친구가 아니에요.");

        ProfileThumbnailImageSource = Profile?.profile_thumbnail_url != null ? new BitmapImage(new Uri(Profile.profile_thumbnail_url)) : (Profile?.profile_image_url != null ? new BitmapImage(new Uri(Profile.profile_image_url)) : null);
        ProfileImageSource = Profile?.profile_image_url != null ? new BitmapImage(new Uri(Profile.profile_image_url)) : null;
        BackgroundImageSource = Profile?.bg_image_url != null ? new BitmapImage(new Uri(Profile.bg_image_url)) : null;
    }

    // Re-fetches the profile without activities so relationship changes
    // (friend/favorite/ban) render immediately.
    public async Task RefreshAsync()
    {
        var profileObject = await _baseViewModel.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.GetProfileFeed(KakaoUserId, null, true));
        if (profileObject?.profile == null) return;

        Profile = profileObject.profile;
        MutualFriend = profileObject.mutual_friend;
        UpdateSurface();
    }

    public override async Task HandleFriendshipActionAsync()
    {
        if (KakaoUserId == null) return;

        if (Profile.relationship == "F")
        {
            var delete = await _baseViewModel.ShowMessageDialogAsync(new("안내", $"{Nickname}님과의 친구 관계를 끊으시겠습니까?", DialogHelper.DefaultOkButtonText, DialogHelper.DefaultCancelButtonText));
            if (delete != ContentDialogResult.Primary) return;

            try
            {
                await _baseViewModel.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.DeleteFriend(KakaoUserId));
                await RefreshAsync();
            }
            catch (Exception exception) { await _baseViewModel.ShowMessageDialogAsync(new(Constants.ErrorTitle, $"친구 삭제에 실패하였습니다.\n{exception.Message}")); }
        }
        else if (Profile.relationship == "R")
        {
            var cancel = await _baseViewModel.ShowMessageDialogAsync(new("안내", $"{Nickname}님에게 보낸 친구 신청을 취소하시겠습니까?", DialogHelper.DefaultOkButtonText, DialogHelper.DefaultCancelButtonText));
            if (cancel != ContentDialogResult.Primary) return;

            try
            {
                await _baseViewModel.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.RequestFriend(KakaoUserId, true));
                await RefreshAsync();
            }
            catch (Exception exception) { await _baseViewModel.ShowMessageDialogAsync(new(Constants.ErrorTitle, $"친구 신청 취소에 실패하였습니다.\n{exception.Message}")); }
        }
        else if (Profile.relationship == "C")
        {
            var action = await _baseViewModel.ShowSelectionDialogAsync("친구 신청", ["수락", "거절"]);
            if (action == null) return;

            try
            {
                await _baseViewModel.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.AcceptFriendRequest(KakaoUserId, action == "거절"));
                await RefreshAsync();
            }
            catch (Exception exception) { await _baseViewModel.ShowMessageDialogAsync(new(Constants.ErrorTitle, $"친구 신청 처리에 실패하였습니다.\n{exception.Message}")); }
        }
        else
        {
            var send = await _baseViewModel.ShowMessageDialogAsync(new("안내", $"{Nickname}님에게 친구 신청을 보내시겠습니까?", DialogHelper.DefaultOkButtonText, DialogHelper.DefaultCancelButtonText));
            if (send != ContentDialogResult.Primary) return;

            try
            {
                await _baseViewModel.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.RequestFriend(KakaoUserId, false));
                await RefreshAsync();
            }
            catch (Exception exception) { await _baseViewModel.ShowMessageDialogAsync(new(Constants.ErrorTitle, $"친구 신청에 실패하였습니다.\n{exception.Message}")); }
        }
    }

    public override async Task HandleFavoriteAsync()
    {
        try
        {
            await _baseViewModel.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.RequestFavorite(KakaoUserId, IsFavorite));
            await RefreshAsync();
        }
        catch (Exception exception) { await _baseViewModel.ShowMessageDialogAsync(new(Constants.ErrorTitle, $"즐겨찾기 처리에 실패하였습니다.\n{exception.Message}")); }
    }

    public override async Task HandleBanAsync()
    {
        if (IsBanned)
        {
            var unban = await _baseViewModel.ShowMessageDialogAsync(new("안내", $"정말로 {Nickname}님의 차단을 해제하시겠습니까? 차단을 해제하면 상대방의 프로필과 글을 다시 볼 수 있습니다.", DialogHelper.DefaultOkButtonText, DialogHelper.DefaultCancelButtonText));
            if (unban != ContentDialogResult.Primary) return;

            try
            {
                await _baseViewModel.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.UnbanProfile(KakaoUserId));
                await RefreshAsync();
            }
            catch (Exception exception) { await _baseViewModel.ShowMessageDialogAsync(new(Constants.ErrorTitle, $"차단 해제에 실패하였습니다.\n{exception.Message}")); }
        }
        else
        {
            var ban = await _baseViewModel.ShowMessageDialogAsync(new("안내", $"정말로 {Nickname}님을 차단하시겠습니까? 차단하는 경우, 해제할 때까지 카카오스토리에서 나와 상대방 모두 서로를 볼 수 없게 됩니다. 또한, 친구 관계인 경우 친구 삭제가 먼저 선행됩니다.", DialogHelper.DefaultOkButtonText, DialogHelper.DefaultCancelButtonText));
            if (ban != ContentDialogResult.Primary) return;

            try
            {
                await _baseViewModel.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.BanProfile(KakaoUserId));
                await RefreshAsync();
            }
            catch (Exception exception) { await _baseViewModel.ShowMessageDialogAsync(new(Constants.ErrorTitle, $"차단에 실패하였습니다.\n{exception.Message}")); }
        }
    }

    // Shows the checkmark glyph on the button for two seconds after copying and
    // ignores re-taps while that feedback is active.
    public override async Task HandleCopyProfileLinkAsync()
    {
        if (_isCopyProfileLinkFeedbackActive) return;

        var permalink = Profile?.permalink;
        if (string.IsNullOrEmpty(permalink))
        {
            await _baseViewModel.ShowMessageDialogAsync(new("안내", "이 프로필은 URL이 존재하지 않습니다."));
            return;
        }

        var dataPackage = new DataPackage { RequestedOperation = DataPackageOperation.Copy };
        dataPackage.SetText(permalink);
        Clipboard.SetContent(dataPackage);

        _isCopyProfileLinkFeedbackActive = true;
        CopyProfileLinkGlyph = CheckMarkGlyph;
        await Task.Delay(2000);
        CopyProfileLinkGlyph = LinkGlyph;
        _isCopyProfileLinkFeedbackActive = false;
    }

    // Opens the profile settings picker (my profile only): nickname, description,
    // profile/background media and mirroring.
    public override async Task HandleProfileSettingsAsync()
    {
        var action = await _baseViewModel.ShowSelectionDialogAsync("프로필 설정", ["닉네임 변경", "한줄 소개 변경", "프로필 이미지 설정", "배경 이미지 설정", "프로필 미러링"]);
        if (action == null) return;

        if (action == "닉네임 변경") await ChangeNicknameAsync();
        else if (action == "한줄 소개 변경") await ChangeDescriptionAsync();
        else if (action == "프로필 이미지 설정") await ChangeProfileMediaAsync();
        else if (action == "배경 이미지 설정") await ChangeBackgroundMediaAsync();
        else if (action == "프로필 미러링") await ChangeProfileMirroringAsync();
    }

    private async Task ChangeNicknameAsync()
    {
        var nickname = await _baseViewModel.ShowInputDialogAsync(new("닉네임 변경", "새로운 닉네임을 입력해주세요.", placeholderText: "새로운 닉네임", showCancel: true, defaultText: Nickname, maxLength: CommonConstants.MaxNicknameLength));
        nickname = nickname?.Trim();
        if (nickname == null || nickname == Nickname) return;

        if (string.IsNullOrWhiteSpace(nickname))
        {
            await _baseViewModel.ShowMessageDialogAsync(new("닉네임 변경 실패", "닉네임은 공백으로 설정할 수 없습니다."));
            return;
        }
        else if (nickname.Length > CommonConstants.MaxNicknameLength)
        {
            await _baseViewModel.ShowMessageDialogAsync(new("닉네임 변경 실패", $"닉네임은 {CommonConstants.MaxNicknameLength}자 이하로 설정할 수 있습니다."));
            return;
        }

        try
        {
            await _baseViewModel.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.SetProfileName(nickname));
            await RefreshAsync();
        }
        catch (Exception exception) { await _baseViewModel.ShowMessageDialogAsync(new(Constants.ErrorTitle, $"닉네임 변경에 실패하였습니다.\n{exception.Message}")); }
    }

    private async Task ChangeDescriptionAsync()
    {
        var description = await _baseViewModel.ShowInputDialogAsync(new("한줄 소개 변경", "새로운 한줄 소개를 입력해주세요. (공백 시 설정 해제)", placeholderText: "새로운 한줄 소개 (공백 시 설정 해제)", showCancel: true, defaultText: Description, maxLength: CommonConstants.MaxProfileDescriptionLength));
        description = description?.Trim();
        if (description == null || description == Description) return;

        if (description.Length > CommonConstants.MaxProfileDescriptionLength)
        {
            await _baseViewModel.ShowMessageDialogAsync(new("한줄 소개 변경 실패", $"한줄 소개는 {CommonConstants.MaxProfileDescriptionLength}자 이하로 설정할 수 있습니다."));
            return;
        }

        try
        {
            await _baseViewModel.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.SetStatusMessage(description));
            await RefreshAsync();
        }
        catch (Exception exception) { await _baseViewModel.ShowMessageDialogAsync(new(Constants.ErrorTitle, $"한줄 소개 변경에 실패하였습니다.\n{exception.Message}")); }
    }

    private async Task ChangeProfileMediaAsync()
    {
        if (HasProfileImage)
        {
            var action = await _baseViewModel.ShowSelectionDialogAsync("프로필 이미지", ["프로필 이미지 변경", "프로필 이미지 삭제"]);
            if (action == null) return;
            else if (action == "프로필 이미지 삭제")
            {
                try
                {
                    await _baseViewModel.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.DeleteProfileImage());
                    await RefreshAsync();
                }
                catch (Exception exception) { await _baseViewModel.ShowMessageDialogAsync(new(Constants.ErrorTitle, $"프로필 이미지 삭제에 실패하였습니다.\n{exception.Message}")); }
                return;
            }
        }

        await UploadAndApplyMediaAsync("프로필 이미지 선택", KakaoStoryApiHandler.SetProfileImage, "프로필 이미지 변경에 실패하였습니다.");
    }

    private async Task ChangeBackgroundMediaAsync()
    {
        if (HasBackgroundImage)
        {
            var action = await _baseViewModel.ShowSelectionDialogAsync("배경 이미지", ["배경 이미지 변경", "배경 이미지 삭제"]);
            if (action == null) return;
            else if (action == "배경 이미지 삭제")
            {
                try
                {
                    await _baseViewModel.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.DeleteBackgroundImage());
                    await RefreshAsync();
                }
                catch (Exception exception) { await _baseViewModel.ShowMessageDialogAsync(new(Constants.ErrorTitle, $"배경 이미지 삭제에 실패하였습니다.\n{exception.Message}")); }
                return;
            }
        }

        await UploadAndApplyMediaAsync("배경 이미지 선택", KakaoStoryApiHandler.SetBackgroundImage, "배경 이미지 변경에 실패하였습니다.");
    }

    // Picks an image, converts it to a Kakao Story-supported format when needed,
    // uploads it and applies it through the given profile API.
    private async Task UploadAndApplyMediaAsync(string pickerTitle, Func<string, Task<UserProfile.ProfileData>> applyImage, string failureMessage)
    {
        var pickResult = await _baseViewModel.PickImageAsync(pickerTitle);
        if (pickResult == null) return;

        var fileName = Path.GetFileName(pickResult.Path);
        var imageData = await File.ReadAllBytesAsync(pickResult.Path);

        var extension = Path.GetExtension(fileName);
        if (KakaoStoryUtils.IsKakaoStoryUnsupportedImageFormat(fileName))
        {
            imageData = ImageConversionHelper.ConvertToPng(imageData);
            if (imageData == null)
            {
                await _baseViewModel.ShowMessageDialogAsync(new(Constants.ErrorTitle, "이미지를 카카오스토리 지원 형식으로 변환하지 못했습니다."));
                return;
            }
            extension = ".png";
        }

        var tempFilePath = Path.Combine(Path.GetTempPath(), $"kakaostory_profile_{Guid.NewGuid():N}{extension}");
        try
        {
            await File.WriteAllBytesAsync(tempFilePath, imageData);
            var mediaPath = await _baseViewModel.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.UploadImage(tempFilePath));
            await _baseViewModel.ExecuteWithLoadingAsync(() => applyImage(mediaPath));
            await RefreshAsync();
        }
        catch (Exception exception) { await _baseViewModel.ShowMessageDialogAsync(new(Constants.ErrorTitle, $"{failureMessage}\n{exception.Message}")); }
        finally { TryDeleteTempFile(tempFilePath); }
    }

    // Mirrors the Kakao Story profile photo and/or background to the History profile:
    // downloads the Kakao Story image and uploads it through the History profile media APIs.
    private async Task ChangeProfileMirroringAsync()
    {
        var action = await _baseViewModel.ShowSelectionDialogAsync("프로필 미러링", ["프로필 사진 미러링", "배경 사진 미러링", "둘 다 미러링"]);
        if (action == null) return;

        var mirrorProfile = action == "프로필 사진 미러링" || action == "둘 다 미러링";
        var mirrorBackground = action == "배경 사진 미러링" || action == "둘 다 미러링";

        if (mirrorProfile && !HasProfileImage)
        {
            await _baseViewModel.ShowMessageDialogAsync(new("안내", "카카오스토리에 프로필 사진이 없어 미러링할 수 없습니다."));
            return;
        }
        if (mirrorBackground && !HasBackgroundImage)
        {
            await _baseViewModel.ShowMessageDialogAsync(new("안내", "카카오스토리에 배경 사진이 없어 미러링할 수 없습니다."));
            return;
        }

        try
        {
            if (mirrorProfile)
            {
                using var httpClient = new HttpClient();
                var imageData = await httpClient.GetByteArrayAsync(Profile.profile_image_url2 ?? Profile.profile_image_url);
                var result = await _baseViewModel.ExecuteRequestAsync(new UpdateProfileMedia("profile.png", imageData));
                if (result.IsFailure) return;
            }

            if (mirrorBackground)
            {
                using var httpClient = new HttpClient();
                var imageData = await httpClient.GetByteArrayAsync(Profile.bg_image_url2 ?? Profile.bg_image_url);
                var result = await _baseViewModel.ExecuteRequestAsync(new UpdateBackgroundMedia("background.png", imageData));
                if (result.IsFailure) return;
            }

            await _baseViewModel.ShowMessageDialogAsync(new("안내", "히스토리 프로필에 미러링되었습니다."));
            await RefreshAsync();
        }
        catch (Exception exception) { await _baseViewModel.ShowMessageDialogAsync(new(Constants.ErrorTitle, $"프로필 미러링에 실패하였습니다.\n{exception.Message}")); }
    }

    private static void TryDeleteTempFile(string filePath)
    {
        try { File.Delete(filePath); }
        catch { }
    }

    public override void HandleProfileTap(string parameter)
    {
        if (parameter == "Navigate")
        {
            if (KakaoUserId == null) return;

            _baseViewModel.RequestNavigation(typeof(ProfilePage), new KakaoProfileParameters(KakaoUserId));
        }
        else
        {
            // TODO: Open the profile image in the full-screen media viewer once it is implemented.
        }
    }

    // TODO: Open the background image in the full-screen media viewer once it is implemented.
    public override void HandleBackgroundTap() { }

    private string GetFriendButtonText()
    {
        if (IsMe) return "ERROR";
        else if (Profile?.relationship == "F") return "친구 삭제";
        else if (Profile?.relationship == "R") return "친구 요청 취소";
        else if (Profile?.relationship == "C") return "친구 수락";
        else return "친구 신청";
    }
}
