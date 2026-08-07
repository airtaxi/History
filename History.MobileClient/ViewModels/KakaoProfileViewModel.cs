using CommunityToolkit.Mvvm.ComponentModel;
using History.Commons;
using History.MobileClient.Helpers;
using History.MobileClient.KakaoStory;
using History.MobileClient.Pages;
using Microsoft.Maui.Graphics.Platform;
using NativeMedia;
using static History.MobileClient.KakaoStory.KakaoStoryApiHandler.DataType;

namespace History.MobileClient.ViewModels;

// Kakao Story profile view model: fills the shared profile surface from the profile feed
// response (ProfileData.Profile + MutualFriend). Relationship actions (friend request/
// accept/delete, favorite, feed block) call the Kakao Story API directly.
public partial class KakaoProfileViewModel : BaseProfileViewModel
{
    private const string DefaultBackgroundImageUrlPrefix = "https://t1.kakaocdn.net/story_static/public/images/wallpapers/";

    [ObservableProperty]
    public partial ProfileData.Profile Profile { get; private set; }

    public string KakaoUserId => Profile?.id;
    public ProfileData.MutualFriend MutualFriend { get; private set; }

    // A profile image is considered missing when the default image is in use.
    public bool HasProfileImage => Profile != null && Profile.is_default_profile_image != true;

    // A background image is considered missing when the default wallpaper is in use.
    public bool HasBackgroundImage => Profile?.bg_image_url != null && !Profile.bg_image_url.StartsWith(DefaultBackgroundImageUrlPrefix, StringComparison.Ordinal);

    // A user banned by me (차단) — distinct from Profile.blocked (Kakao-suspended user).
    public bool IsBanned => Profile?.relation?.ban == "A";

    // The friends button is shown when the profile exposes a friend list (friend_count > 0).
    public bool IsFriendsVisible => !IsMe && (Profile?.friend_count ?? 0) > 0;

    public KakaoProfileViewModel(ProfileData.Profile profile, ProfileData.MutualFriend mutualFriend)
    {
        Profile = profile;
        MutualFriend = mutualFriend;
        UpdateSurface();
    }

    private void UpdateSurface()
    {
        IsMe = Profile?.id == Shared.KakaoUserId;
        IsNotMe = !IsMe;
        IsFriend = Profile?.relationship == "F";
        IsFavorite = Profile?.is_favorite ?? false;
        FavoriteColor = IsFavorite ? Application.Current.Resources["Primary"] as Color : Color.FromRgb(0x30, 0x30, 0x30);
        FriendButtonText = GetFriendButtonText();
        Nickname = Profile?.display_name;
        Description = Profile?.status_objects?.FirstOrDefault()?.message ?? "설정된 한줄 소개가 없습니다";
        FriendshipDescription = IsMe ? "내 프로필입니다." : (MutualFriend?.message ?? "친구가 아니에요.");
        BackgroundMedia = Profile?.bg_image_url != null ? new ImageViewModel(Profile.bg_image_url) : null;
        ProfileMedia = Profile?.profile_image_url != null ? new ImageViewModel(Profile.profile_image_url) : null;
        IsBlocked = Profile?.blocked ?? false;
        BlockedUserIdText = $"사용자 ID: {Profile?.id}";
        IsFeedBlockAvailable = !IsMe && !IsBlocked;
        FeedBlockButtonText = (Profile?.is_feed_blocked ?? false) ? $"'{Nickname}' 글 받기" : $"'{Nickname}' 글 안받기";
    }
    private string GetFriendButtonText()
    {
        if (IsMe) return "ERROR";
        else if (Profile?.relationship == "F") return "친구 삭제";
        else if (Profile?.relationship == "R") return "친구 요청 취소";
        else if (Profile?.relationship == "C") return "친구 수락";
        else return "친구 신청";
    }

    public override async Task RefreshAsync()
    {
        var profileObject = await App.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.GetProfileFeed(KakaoUserId, null, true));
        if (profileObject?.profile == null) return;

        Profile = profileObject.profile;
        MutualFriend = profileObject.mutual_friend;
        UpdateSurface();
    }

    public override async Task HandleProfileTapAsync()
    {
        if (Profile?.profile_image_url2 == null)
        {
            await App.Page.DisplayAlertAsync("안내", "프로필 이미지가 없습니다.", Constants.PromptOk);
            return;
        }

        var media = new ImageViewModel(Profile.profile_image_url2)
        {
            Aspect = Aspect.AspectFit,
            HorizontalContentOptions = LayoutOptions.Fill,
            VerticalContentOptions = LayoutOptions.Fill,
            FullScreenSwipeable = false,
            IsFullScreen = true,
        };

        var viewerPage = new FullScreenMediaViewerPage(new FullScreenMediaContentViewModel([media], media));
        await App.PushAsync(viewerPage);
    }

    public override async Task HandleProfileLongPressAsync() { }

    public override async Task HandleBackgroundTapAsync()
    {
        if (Profile?.bg_image_url2 == null)
        {
            await App.Page.DisplayAlertAsync("안내", "배경 이미지가 없습니다.", Constants.PromptOk);
            return;
        }

        var media = new ImageViewModel(Profile.bg_image_url2)
        {
            Aspect = Aspect.AspectFit,
            HorizontalContentOptions = LayoutOptions.Fill,
            VerticalContentOptions = LayoutOptions.Fill,
            FullScreenSwipeable = false,
            IsFullScreen = true,
        };

        var viewerPage = new FullScreenMediaViewerPage(new FullScreenMediaContentViewModel([media], media));
        await App.PushAsync(viewerPage);
    }

    public override async Task HandleFriendshipActionAsync()
    {
        if (Profile?.relationship == "F")
        {
            var delete = await App.Page.DisplayAlertAsync("안내", $"{Nickname}님와의 친구 관계를 끊으시겠습니까?", Constants.PromptYes, Constants.PromptNo);
            if (!delete) return;

            try
            {
                await App.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.DeleteFriend(KakaoUserId));
                await RefreshAsync();
            }
            catch (Exception exception) { await App.Page.DisplayAlertAsync("오류", $"친구 삭제에 실패하였습니다.\n{exception.Message}", Constants.PromptOk); }
        }
        else if (Profile?.relationship == "R")
        {
            var cancel = await App.Page.DisplayAlertAsync("안내", $"{Nickname}님에게 보낸 친구 신청을 취소하시겠습니까?", Constants.PromptYes, Constants.PromptNo);
            if (!cancel) return;

            try
            {
                await App.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.RequestFriend(KakaoUserId, true));
                await RefreshAsync();
            }
            catch (Exception exception) { await App.Page.DisplayAlertAsync("오류", $"친구 신청 취소에 실패하였습니다.\n{exception.Message}", Constants.PromptOk); }
        }
        else if (Profile?.relationship == "C")
        {
            var action = await App.Page.DisplayActionSheetAsync("친구 신청", Constants.PromptCancel, null, "수락", "거절");
            if (action == null || action == Constants.PromptCancel) return;

            try
            {
                await App.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.AcceptFriendRequest(KakaoUserId, action == "거절"));
                await RefreshAsync();
            }
            catch (Exception exception) { await App.Page.DisplayAlertAsync("오류", $"친구 신청 처리에 실패하였습니다.\n{exception.Message}", Constants.PromptOk); }
        }
        else
        {
            var send = await App.Page.DisplayAlertAsync("안내", $"{Nickname}님에게 친구 신청을 보내시겠습니까?", Constants.PromptYes, Constants.PromptNo);
            if (!send) return;

            try
            {
                await App.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.RequestFriend(KakaoUserId, false));
                await RefreshAsync();
            }
            catch (Exception exception) { await App.Page.DisplayAlertAsync("오류", $"친구 신청에 실패하였습니다.\n{exception.Message}", Constants.PromptOk); }
        }
    }

    public override async Task HandleFavoriteAsync()
    {
        try
        {
            await App.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.RequestFavorite(KakaoUserId, IsFavorite));
            await RefreshAsync();
        }
        catch (Exception exception) { await App.Page.DisplayAlertAsync("오류", $"즐겨찾기 처리에 실패하였습니다.\n{exception.Message}", Constants.PromptOk); }
    }

    public override async Task HandleProfileSettingsAsync()
    {
        var action = await App.Page.DisplayActionSheetAsync("프로필 설정", Constants.PromptCancel, null, "닉네임 변경", "한줄 소개 변경", "프로필 이미지 설정", "배경 이미지 설정");

        if (action == null || action == Constants.PromptCancel) return;

        if (action == "닉네임 변경") await HandleChangeNicknameAsync();
        else if (action == "한줄 소개 변경") await HandleChangeDescriptionAsync();
        else if (action == "프로필 이미지 설정") await HandleChangeProfileMediaAsync();
        else if (action == "배경 이미지 설정") await HandleChangeBackgroundMediaAsync();
    }

    private async Task HandleChangeNicknameAsync()
    {
        var prompt = await App.Page.DisplayPromptAsync("닉네임 변경", "새로운 닉네임을 입력해주세요", "변경", Constants.PromptCancel, "새로운 닉네임", 40, Keyboard.Plain, Nickname);
        prompt = prompt?.Trim();

        if (prompt != null && prompt != Nickname)
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                await App.Page.DisplayAlertAsync("닉네임 변경 실패", "닉네임은 공백으로 설정할 수 없습니다", Constants.PromptOk);
                return;
            }
            else if (prompt.Length > CommonsConstants.MaxNicknameLength)
            {
                await App.Page.DisplayAlertAsync("닉네임 변경 실패", $"닉네임은 {CommonsConstants.MaxNicknameLength}자 이하로 설정할 수 있습니다", Constants.PromptOk);
                return;
            }

            try
            {
                await App.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.SetProfileName(prompt));
                await RefreshAsync();
            }
            catch (Exception exception) { await App.Page.DisplayAlertAsync("오류", $"닉네임 변경에 실패하였습니다.\n{exception.Message}", Constants.PromptOk); }
        }
    }

    private async Task HandleChangeDescriptionAsync()
    {
        var prompt = await App.Page.DisplayPromptAsync("한줄 소개 변경", "새로운 한줄 소개를 입력해주세요 (공백 시 설정 해제)", "변경", Constants.PromptCancel, "새로운 한줄 소개 (공백 시 설정 해제)", 40, Keyboard.Plain, Description);
        prompt = prompt?.Trim();

        if (prompt != null && prompt != Description)
        {
            if (prompt.Length > CommonsConstants.MaxProfileDescriptionLength)
            {
                await App.Page.DisplayAlertAsync("한줄 소개 변경 실패", $"한줄 소개는 {CommonsConstants.MaxProfileDescriptionLength}자 이하로 설정할 수 있습니다", Constants.PromptOk);
                return;
            }

            try
            {
                await App.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.SetStatusMessage(prompt));
                await RefreshAsync();
            }
            catch (Exception exception) { await App.Page.DisplayAlertAsync("오류", $"한줄 소개 변경에 실패하였습니다.\n{exception.Message}", Constants.PromptOk); }
        }
    }

    private async Task HandleChangeProfileMediaAsync()
    {
        var shouldUpload = true;
        if (HasProfileImage)
        {
            var action = await App.Page.DisplayActionSheetAsync("프로필 이미지", Constants.PromptCancel, null, ["프로필 이미지 변경", "프로필 이미지 삭제"]);
            if (action == Constants.PromptCancel) return;
            else if (action == "프로필 이미지 변경") shouldUpload = true;
            else if (action == "프로필 이미지 삭제")
            {
                try
                {
                    await App.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.DeleteProfileImage());
                    await RefreshAsync();
                }
                catch (Exception exception) { await App.Page.DisplayAlertAsync("오류", $"프로필 이미지 삭제에 실패하였습니다.\n{exception.Message}", Constants.PromptOk); }
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
            bytes = await TryConvertToKakaoSupportedImageAsync(fileName, bytes);
            if (bytes == null) return;

            try
            {
                var tempFilePath = Path.Combine(FileSystem.CacheDirectory, $"profile_image_{Guid.NewGuid():N}.png");
                try
                {
                    await File.WriteAllBytesAsync(tempFilePath, bytes);
                    var imagePath = await App.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.UploadImage(tempFilePath));
                    await App.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.SetProfileImage(imagePath));
                    await RefreshAsync();
                }
                finally { try { File.Delete(tempFilePath); } catch { } }
            }
            catch (Exception exception) { await App.Page.DisplayAlertAsync("오류", $"프로필 이미지 변경에 실패하였습니다.\n{exception.Message}", Constants.PromptOk); }
        }
    }

    private async Task HandleChangeBackgroundMediaAsync()
    {
        var shouldUpload = true;
        if (HasBackgroundImage)
        {
            var action = await App.Page.DisplayActionSheetAsync("배경 이미지", Constants.PromptCancel, null, ["배경 이미지 변경", "배경 이미지 삭제"]);
            if (action == Constants.PromptCancel) return;
            else if (action == "배경 이미지 변경") shouldUpload = true;
            else if (action == "배경 이미지 삭제")
            {
                try
                {
                    await App.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.DeleteBackgroundImage());
                    await RefreshAsync();
                }
                catch (Exception exception) { await App.Page.DisplayAlertAsync("오류", $"배경 이미지 삭제에 실패하였습니다.\n{exception.Message}", Constants.PromptOk); }
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
            bytes = await TryConvertToKakaoSupportedImageAsync(fileName, bytes);
            if (bytes == null) return;

            try
            {
                var tempFilePath = Path.Combine(FileSystem.CacheDirectory, $"background_image_{Guid.NewGuid():N}.png");
                try
                {
                    await File.WriteAllBytesAsync(tempFilePath, bytes);
                    var imagePath = await App.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.UploadImage(tempFilePath));
                    await App.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.SetBackgroundImage(imagePath));
                    await RefreshAsync();
                }
                finally { try { File.Delete(tempFilePath); } catch { } }
            }
            catch (Exception exception) { await App.Page.DisplayAlertAsync("오류", $"배경 이미지 변경에 실패하였습니다.\n{exception.Message}", Constants.PromptOk); }
        }
    }

    /// <summary>
    /// Converts the picked image to PNG when Kakao Story does not accept the format.
    /// WebP is converted (Kakao Story does not support it); GIF is rejected because
    /// only static images are allowed. Returns null when the image cannot be used.
    /// </summary>
    private static async Task<byte[]> TryConvertToKakaoSupportedImageAsync(string fileName, byte[] bytes)
    {
        if (fileName.EndsWith(".gif", StringComparison.OrdinalIgnoreCase))
        {
            await App.Page.DisplayAlertAsync("안내", "움직이는 이미지(gif)는 프로필 이미지로 설정할 수 없습니다.", Constants.PromptOk);
            return null;
        }

        if (!fileName.EndsWith(".webp", StringComparison.OrdinalIgnoreCase)) return bytes;

        try
        {
            using var stream = new MemoryStream(bytes);
            using var image = PlatformImage.FromStream(stream);
            if (image == null)
            {
                await App.Page.DisplayAlertAsync("오류", "이미지를 변환할 수 없습니다. 애니메이션이 포함된 webp 이미지일 수 있습니다.", Constants.PromptOk);
                return null;
            }

            using var saveStream = new MemoryStream();
            await image.SaveAsync(saveStream, ImageFormat.Png);
            return saveStream.ToArray();
        }
        catch
        {
            await App.Page.DisplayAlertAsync("오류", "이미지를 변환할 수 없습니다. 애니메이션이 포함된 webp 이미지일 수 있습니다.", Constants.PromptOk);
            return null;
        }
    }

    public override async Task HandleBanAsync()
    {
        if (IsBanned)
        {
            var unban = await App.Page.DisplayAlertAsync("안내", $"정말로 {Nickname}님의 차단을 해제하시겠습니까? 차단을 해제하면 상대방의 프로필과 글을 다시 볼 수 있습니다.", Constants.PromptYes, Constants.PromptNo);
            if (!unban) return;

            try
            {
                await App.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.UnbanProfile(KakaoUserId));
                await RefreshAsync();
            }
            catch (Exception exception) { await App.Page.DisplayAlertAsync("오류", $"차단 해제에 실패하였습니다.\n{exception.Message}", Constants.PromptOk); }
        }
        else
        {
            var ban = await App.Page.DisplayAlertAsync("안내", $"정말로 {Nickname}님을 차단하시겠습니까? 차단하는 경우, 해제할 때 까지 카카오스토리에서 나와 상대방 모두 서로를 볼 수 없게 됩니다. 또한, 친구 관계인 경우 친구 삭제가 먼저 선행됩니다.", Constants.PromptYes, Constants.PromptNo);
            if (!ban) return;

            try
            {
                await App.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.BanProfile(KakaoUserId));
                await RefreshAsync();
            }
            catch (Exception exception) { await App.Page.DisplayAlertAsync("오류", $"차단에 실패하였습니다.\n{exception.Message}", Constants.PromptOk); }
        }
    }

    public override async Task HandleFeedBlockAsync()
    {
        var isUnblock = Profile?.is_feed_blocked ?? false;
        var confirm = await App.Page.DisplayAlertAsync("안내", isUnblock ? $"'{Nickname}'님의 글을 다시 받으시겠습니까?" : $"'{Nickname}'님의 글을 더 이상 받지 않으시겠습니까?", Constants.PromptYes, Constants.PromptNo);
        if (!confirm) return;

        try
        {
            await App.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.BlockProfile(KakaoUserId, isUnblock));
            await RefreshAsync();
        }
        catch (Exception exception) { await App.Page.DisplayAlertAsync("오류", $"피드 차단 처리에 실패하였습니다.\n{exception.Message}", Constants.PromptOk); }
    }
}
