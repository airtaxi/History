using CommunityToolkit.Mvvm.ComponentModel;
using History.Commons;
using History.MobileClient.KakaoStory;
using History.MobileClient.Pages;
using static History.MobileClient.KakaoStory.KakaoStoryApiHandler.DataType;

namespace History.MobileClient.ViewModels;

// Kakao Story profile view model: fills the shared profile surface from the profile feed
// response (ProfileData.Profile + MutualFriend). Relationship actions (friend request/
// accept/delete, favorite, feed block) call the Kakao Story API directly.
public partial class KakaoProfileViewModel : BaseProfileViewModel
{
    [ObservableProperty]
    public partial ProfileData.Profile Profile { get; private set; }

    public string KakaoUserId => Profile?.id;
    public ProfileData.MutualFriend MutualFriend { get; private set; }

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
        IsProfileSettingsVisible = false;
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

    public override async Task HandleProfileSettingsAsync() { }

    public override async Task HandleBanAsync() { }

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
