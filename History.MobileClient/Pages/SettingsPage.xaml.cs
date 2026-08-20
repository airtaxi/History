using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using History.Commons;
using History.Commons.Api.User;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using History.MobileClient.DataTypes;
using History.MobileClient.Messages;
using History.MobileClient.Helpers;
using History.MobileClient.KakaoStory;

namespace History.MobileClient.Pages;

public partial class SettingsPage : ContentPage
{
    private bool _isInForeground;

    private UserResponseDto _user;

    private const string OnText = "켜짐";
    private const string OffText = "꺼짐";

    public SettingsPage(UserResponseDto user)
	{
        _user = user;
        InitializeComponent();

        VersionLabel.Text = AppInfo.Current.VersionString;

        var splittedBirthday = user.Birthday?.Split('-') ?? [];
        if (splittedBirthday.Length == 2)
        {
            var month = splittedBirthday[0];
            var day = splittedBirthday[1];
            BirthdayLabel.Text = $"{month}월 {day}일";
        }
        else BirthdayLabel.Text = "설정되지 않음";

        FriendListDiscovryOptionLabel.Text = user.FriendListDiscoveryOption.ToDisplayString();

        // push notification permission
        CommentPushNotificationPermissionLabel.Text = user.CommentPushNotificationPermission.ToDisplayString().Replace(AccessPermission.OnlyMe.ToDisplayString(), OffText);
        CommentMentionPushNotificationPermissionLabel.Text = user.CommentMentionPushNotificationPermission.ToDisplayString().Replace(AccessPermission.OnlyMe.ToDisplayString(), OffText);
        CommentLikePushNotificationPermissionLabel.Text = user.CommentLikePushNotificationPermission.ToDisplayString().Replace(AccessPermission.OnlyMe.ToDisplayString(), OffText);
        SharedPostCommentPushNotificationPermissionLabel.Text = user.SharedPostCommentPushNotificationPermission.ToDisplayString().Replace(AccessPermission.OnlyMe.ToDisplayString(), OffText);
        SharedPostCommentPushNotificationPermissionLabel.Text = user.SharedPostCommentPushNotificationPermission.ToDisplayString().Replace(AccessPermission.OnlyMe.ToDisplayString(), OffText);
        PostReactionPushNotificationPermissionLabel.Text = user.PostReactionPushNotificationPermission.ToDisplayString().Replace(AccessPermission.OnlyMe.ToDisplayString(), OffText);
        PostMentionPushNotificationPermissionLabel.Text = user.PostMentionPushNotificationPermission.ToDisplayString().Replace(AccessPermission.OnlyMe.ToDisplayString(), OffText);
        IsFavoriteFriendNewPostPushNotificationEnabledLabel.Text = user.IsFavoriteFriendNewPostPushNotificationEnabled ? OnText : OffText;

        var theme = Configuration.GetValue<string>("Theme");
        ThemeLabel.Text = theme switch
        {
            "Light" => "라이트 모드",
            "Dark" => "다크 모드",
            _ => "시스템 설정 따름"
        };

        var isKakaoStoryProfanityCheckEnabled = Configuration.GetValue<bool?>("KakaoStoryProfanityCheckEnabled") ?? true;
        KakaoStoryProfanityCheckLabel.Text = isKakaoStoryProfanityCheckEnabled ? OnText : OffText;

        var isKakaoStoryNotificationEnabled = Configuration.GetValue<bool?>("KakaoStoryNotificationEnabled") ?? true;
        KakaoStoryNotificationLabel.Text = isKakaoStoryNotificationEnabled ? OnText : OffText;

        var isKakaoStoryFavoriteFriendNotificationEnabled = Configuration.GetValue<bool?>("KakaoStoryFavoriteFriendNotificationEnabled") ?? true;
        KakaoStoryFavoriteFriendNotificationLabel.Text = isKakaoStoryFavoriteFriendNotificationEnabled ? OnText : OffText;

        var isKakaoStoryEmotionNotificationEnabled = Configuration.GetValue<bool?>("KakaoStoryEmotionNotificationEnabled") ?? true;
        KakaoStoryEmotionNotificationLabel.Text = isKakaoStoryEmotionNotificationEnabled ? OnText : OffText;

        var isKakaoStoryMailNotificationEnabled = Configuration.GetValue<bool?>("KakaoStoryMailNotificationEnabled") ?? true;
        KakaoStoryMailNotificationLabel.Text = isKakaoStoryMailNotificationEnabled ? OnText : OffText;

        var isKakaoStorySessionExpiredNotificationEnabled = Configuration.GetValue<bool?>("KakaoStorySessionExpiredNotificationEnabled") ?? true;
        KakaoStorySessionExpiredNotificationLabel.Text = isKakaoStorySessionExpiredNotificationEnabled ? OnText : OffText;

        var isKakaoStoryNotificationBadgeEnabled = Configuration.GetValue<bool?>("KakaoStoryNotificationBadgeEnabled") ?? true;
        KakaoStoryNotificationBadgeLabel.Text = isKakaoStoryNotificationBadgeEnabled ? OnText : OffText;

        var isKakaoStoryMailBadgeEnabled = Configuration.GetValue<bool?>("KakaoStoryMailBadgeEnabled") ?? true;
        KakaoStoryMailBadgeLabel.Text = isKakaoStoryMailBadgeEnabled ? OnText : OffText;

        var isKakaoStoryFriendRequestBadgeEnabled = Configuration.GetValue<bool?>("KakaoStoryFriendRequestBadgeEnabled") ?? true;
        KakaoStoryFriendRequestBadgeLabel.Text = isKakaoStoryFriendRequestBadgeEnabled ? OnText : OffText;

        KakaoStoryForegroundPollIntervalLabel.Text = FormatPollInterval(Configuration.GetValue<double?>(KakaoStoryNotificationPoller.ForegroundPollIntervalSecondsKey) ?? 5.0);

#if ANDROID
        KakaoStoryRealtimeScreenOnPollIntervalLabel.Text = FormatPollInterval(Configuration.GetValue<double?>(KakaoStoryRealtimeNotificationService.ScreenOnPollIntervalSecondsKey) ?? 40.0);
        KakaoStoryRealtimeScreenOffPollIntervalLabel.Text = FormatPollInterval(Configuration.GetValue<double?>(KakaoStoryRealtimeNotificationService.ScreenOffPollIntervalSecondsKey) ?? 300.0);
#endif

#if ANDROID
        // Virtualization toggle (default: off for smoother scroll with less View recreation)
        var isTimelineVirtualizationEnabled = Configuration.GetValue<bool?>("TimelineVirtualizationEnabled") ?? false;
        TimelineVirtualizationLabel.Text = isTimelineVirtualizationEnabled ? OnText : OffText;

        // Realtime Kakao Story notification foreground service (default: off)
        KakaoStoryRealtimeNotificationLabel.Text = KakaoStoryRealtimeNotificationManager.IsEnabled ? OnText : OffText;
#endif

        WeakReferenceMessenger.Default.Register<LoadingStateChangedMessage>(this, OnLoadingStateChangedMessageReceived);
    }

    private static string FormatPollInterval(double seconds) => seconds < 60 ? $"{seconds:0}초" : $"{seconds / 60:0}분";

    private static void CleanupSharedVariables()
    {
        Configuration.SetValue("AccessToken", null);
        Configuration.SetValue("RefreshToken", null);

        Shared.ApiHandler = ApiHandler.Public;
        Shared.UserId = default;
        Shared.MyRank = default;
        Shared.LastUsedPostDiscoveryOption = default;
        Shared.Friends = default;
        Shared.KakaoFriends = default;
        Shared.HistoryUnreadNotificationCount = 0;
        Shared.KakaoStoryUnreadNotificationCount = 0;
        Shared.HistoryUnreadMailCount = 0;
        Shared.KakaoStoryUnreadMailCount = 0;
        Shared.HistoryPendingFriendRequestCount = 0;
        Shared.KakaoStoryPendingFriendRequestCount = 0;

        // Pause the foreground pollers so the counts stay reset until login.
        TabBarBadgePoller.Pause();
        KakaoStoryNotificationPoller.Pause();
#if ANDROID
        // The realtime notification service polls Kakao Story, which is
        // meaningless while logged out.
        KakaoStoryRealtimeNotificationManager.Stop();
#endif
    }

    private async Task SetupPushNotificationPermission(PushNotificationType type)
    {
        var accessPermissions = Enum.GetValues<AccessPermission>().Select(x => x.ToDisplayString()).ToList();
        var offIndex = accessPermissions.FindIndex(x => x == AccessPermission.OnlyMe.ToDisplayString());
        accessPermissions[offIndex] = OffText;

        var action = await DisplayActionSheetAsync(type.ToDisplayString() + " 푸시 알림", Constants.PromptCancel, null, [.. accessPermissions]);
        if (action == null || action == Constants.PromptCancel) return;

        var selectedIndex = accessPermissions.IndexOf(action);
        if (selectedIndex < 0 || selectedIndex >= accessPermissions.Count)
        {
            await DisplayAlertAsync("오류", "잘못된 선택입니다. 다시 시도해주세요.", Constants.PromptOk);
            return;
        }

        var permission = (AccessPermission)selectedIndex;

        var result = await App.ExecuteRequestAsync(new UpdatePushNotificationPermission(type, permission));
        if (result.IsSuccess)
        {
            switch (type)
            {
                case PushNotificationType.Comment:
                    CommentPushNotificationPermissionLabel.Text = permission.ToDisplayString().Replace(AccessPermission.OnlyMe.ToDisplayString(), OffText);
                    _user.CommentPushNotificationPermission = permission;
                    break;
                case PushNotificationType.CommentMention:
                    CommentMentionPushNotificationPermissionLabel.Text = permission.ToDisplayString().Replace(AccessPermission.OnlyMe.ToDisplayString(), OffText);
                    _user.CommentMentionPushNotificationPermission = permission;
                    break;
                case PushNotificationType.CommentLike:
                    CommentLikePushNotificationPermissionLabel.Text = permission.ToDisplayString().Replace(AccessPermission.OnlyMe.ToDisplayString(), OffText);
                    _user.CommentLikePushNotificationPermission = permission;
                    break;
                case PushNotificationType.SharedPostComment:
                    SharedPostCommentPushNotificationPermissionLabel.Text = permission.ToDisplayString().Replace(AccessPermission.OnlyMe.ToDisplayString(), OffText);
                    _user.SharedPostCommentPushNotificationPermission = permission;
                    break;
                case PushNotificationType.PostReaction:
                    PostReactionPushNotificationPermissionLabel.Text = permission.ToDisplayString().Replace(AccessPermission.OnlyMe.ToDisplayString(), OffText);
                    _user.PostReactionPushNotificationPermission = permission;
                    break;
                case PushNotificationType.PostMention:
                    PostMentionPushNotificationPermissionLabel.Text = permission.ToDisplayString().Replace(AccessPermission.OnlyMe.ToDisplayString(), OffText);
                    _user.PostMentionPushNotificationPermission = permission;
                    break;
            }

            await DisplayAlertAsync("안내", $"{type.ToDisplayString()} 푸시 알림 설정이 변경되었습니다.", Constants.PromptOk);
        }
    }

    private async void OnBackImageTapped(object sender, TappedEventArgs e) => await App.PopAsync();

    private async void OnLogoutGridTapped(object sender, TappedEventArgs e)
    {
        var result = await DisplayAlertAsync("안내", "정말로 로그아웃을 하시겠습니까?", "네", "아니오");
        if (!result) return;

        CleanupSharedVariables();

        App.Page = new LoginPage();
    }

    private async void OnWithdrawGridTapped(object sender, TappedEventArgs e)
    {
        var result = await DisplayAlertAsync("안내", "정말로 회원 탈퇴를 하시겠습니까?", "네", "아니오");
        if (!result) return;

        result = await DisplayAlertAsync("경고", "회원 탈퇴는 되돌릴 수 없습니다. 정말로 회원 탈퇴를 하시겠습니까?", "네", "아니오");
        if (!result) return;

        var prompt = await DisplayPromptAsync("경고", "회원 탈퇴를 하시면 이용 약관에 따라 유예 기간 없이 모든 모든 데이터가 삭제됩니다. 이에 동의하시면 아래 \"탈퇴하겠습니다\"를 따옴표 없이 입력해주세요.", "회원 탈퇴", "취소", "탈퇴하려면 \"탈퇴하겠습니다\"를 따옴표 없이 입력");
        if (prompt != "탈퇴하겠습니다") return;

        var response = await App.ExecuteRequestAsync(new Withdraw());
        if (response.IsSuccess)
        {
            CleanupSharedVariables();

            await DisplayAlertAsync("안내", "회원 탈퇴가 완료되었습니다. 이용해 주셔서 감사합니다.", "확인");
            App.Page = new LoginPage();
        }
    }

    private async void OnFriendListDiscovryOptionGridTapped(object sender, TappedEventArgs e)
    {
        var discoveryOptions = Enum.GetValues<DiscoveryOption>().ToList();
        discoveryOptions.Remove(DiscoveryOption.SelectedUsers);
        discoveryOptions.Remove(DiscoveryOption.UnselectedUsers);

        var rawDiscoveryOptions = discoveryOptions.Select(x => x.ToDisplayString()).ToArray();
        var rawDiscoveryOption = await App.Page.DisplayActionSheetAsync("친구 목록 공개 범위 설정", Constants.PromptCancel, null, rawDiscoveryOptions);

        if (rawDiscoveryOption == null || rawDiscoveryOption == Constants.PromptCancel) return;

        var discoveryOption = DiscoveryOptionExtensions.FromDisplayString(rawDiscoveryOption);
        var result = await App.ExecuteRequestAsync(new UpdateFriendListDiscoveryOption(discoveryOption));
        if (result.IsSuccess) FriendListDiscovryOptionLabel.Text = rawDiscoveryOption;
    }

    private static readonly int[] s_months = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12];
    private static readonly int[] s_monthDays = [31, 29, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31];

    private async void OnBirthdayGridTapped(object sender, TappedEventArgs e)
    {
        var action = await DisplayActionSheetAsync("생일 설정", Constants.PromptCancel, _user.Birthday != null ? "생일 삭제" : null, _user.Birthday != null ? "생일 변경" : "생일 추가");
        if (action == null || action == Constants.PromptCancel) return;

        if (action == "생일 추가" || action == "생일 변경")
        {
            var months = s_months.Select(m => $"{m}월").ToArray();
            action = await DisplayActionSheetAsync("월을 선택해주세요", Constants.PromptCancel, null, months);
            if (action == null || action == Constants.PromptCancel) return;

            var month = Array.IndexOf(months, action) + 1;
            var days = Enumerable.Range(1, s_monthDays[month - 1]).Select(d => $"{d}일").ToArray();

            action = await DisplayActionSheetAsync("일을 선택해주세요", Constants.PromptCancel, null, days);
            if (action == null || action == Constants.PromptCancel) return;

            var day = Array.IndexOf(days, action) + 1;

            if (month < 1 || month > 12 || day < 1 || day > s_monthDays[month - 1])
            {
                await DisplayAlertAsync("오류", "잘못된 날짜입니다. 다시 시도해주세요.", Constants.PromptOk);
                return;
            }

            var birthdayDateTime = new DateTime(DateTime.Now.Year, month, day);
            var result = await App.ExecuteRequestAsync(new UpdateBirthday(birthdayDateTime));
            if (result.IsSuccess)
            {
                BirthdayLabel.Text = $"{month}월 {day}일";
                return;
            }
        }
        if (action == "생일 삭제")
        {
            var result = await App.ExecuteRequestAsync(new UpdateBirthday(null));
            if (result.IsSuccess)
            {
                BirthdayLabel.Text = "설정되지 않음";
                return;
            }
        }
    }

    private async void OnTermsGridTapped(object sender, TappedEventArgs e)
    {
#if IOS
        await Browser.Default.OpenAsync("https://history.cenox.io/terms.html", BrowserLaunchMode.SystemPreferred);
#else
        var page = new InAppBrowserPage("서비스 이용 약관", "https://history.cenox.io/terms.html");
        await App.PushModalAsync(page);
#endif
    }

    private async void OnPrivacyPolicyGridTapped(object sender, TappedEventArgs e)
    {
#if IOS
        await Browser.Default.OpenAsync("https://history.cenox.io/privacypolicy.html", BrowserLaunchMode.SystemPreferred);
#else
        var page = new InAppBrowserPage("개인정보처리방침", "https://history.cenox.io/privacypolicy.html");
        await App.PushModalAsync(page);
#endif
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _isInForeground = true;

        var safeAreaTopHeight = LayoutHelper.GetSafeAreaTopHeight();
        if (safeAreaTopHeight != 0)
        {
            var statusBarHeight = LayoutHelper.GetStatusBarHeight();
            Padding = new Thickness(Padding.Left, -(safeAreaTopHeight - statusBarHeight), Padding.Right, Padding.Bottom);
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _isInForeground = false;
    }

    private void OnLoadingStateChangedMessageReceived(object recipient, LoadingStateChangedMessage message)
    {
        var isLoading = message.Value;
        if (!_isInForeground && isLoading) return;

        Application.Current.Dispatcher.Dispatch(() =>
        {
            MainActivityIndicator.IsRunning = isLoading;
            IsEnabled = !isLoading;
        });
    }

    private void OnLoaded(object sender, EventArgs e)
    {
#if IOS
        AppleSwipeGestureHelper.ApplyToPage(this);
#endif
    }

    private async void OnKakaoStoryLoginGridTapped(object sender, TappedEventArgs e)
    {
        var page = new KakaoStoryLoginPage();
        await App.PushModalAsync(page);
    }

    private async void OnKakaoStoryCredentialResetGridTapped(object sender, TappedEventArgs e)
    {
        var savedEmail = Configuration.GetValue<string>("KakaoStoryEmail");
        if (string.IsNullOrEmpty(savedEmail))
        {
            await DisplayAlertAsync("안내", "저장된 카카오스토리 로그인 정보가 없습니다.", Constants.PromptOk);
            return;
        }

        var confirm = await DisplayAlertAsync("확인", "저장된 카카오스토리 로그인 정보를 초기화하시겠습니까?", Constants.PromptOk, Constants.PromptCancel);
        if (!confirm) return;

        Configuration.SetValue("KakaoStoryEmail", null);
        Configuration.SetValue("KakaoStoryPassword", null);
        KakaoStoryApiHandler.ClearSdkTokens();
        await DisplayAlertAsync("안내", "카카오스토리 로그인 정보가 초기화되었습니다.", Constants.PromptOk);
    }

    private async void OnKakaoStoryProfanityCheckGridTapped(object sender, TappedEventArgs e)
    {
        var action = await DisplayActionSheetAsync("카카오스토리 업로드 시 욕설 체크", Constants.PromptCancel, null, OnText, OffText);
        if (action == null || action == Constants.PromptCancel) return;

        var isEnabled = action == OnText;
        Configuration.SetValue("KakaoStoryProfanityCheckEnabled", isEnabled);
        KakaoStoryProfanityCheckLabel.Text = isEnabled ? OnText : OffText;
    }

    private async void OnKakaoStoryNotificationGridTapped(object sender, TappedEventArgs e)
    {
        var action = await DisplayActionSheetAsync("카카오스토리 푸시 알림", Constants.PromptCancel, null, OnText, OffText);
        if (action == null || action == Constants.PromptCancel) return;

        var isEnabled = action == OnText;
        Configuration.SetValue("KakaoStoryNotificationEnabled", isEnabled);
        KakaoStoryNotificationLabel.Text = isEnabled ? OnText : OffText;

        // Start/stop the foreground polling loop so a disabled setting costs no battery.
        if (isEnabled)
        {
            KakaoStoryNotificationPoller.StartForegroundPolling();
#if IOS
            // Re-arm the background refresh so a disable/re-enable cycle without
            // a background transition (the window Stopped event) still polls.
            KakaoStoryBackgroundRefresh.ScheduleNext();
#endif
        }
        else KakaoStoryNotificationPoller.StopForegroundPolling();
    }

#if ANDROID
    private async void OnKakaoStoryRealtimeNotificationGridTapped(object sender, TappedEventArgs e)
    {
        var action = await DisplayActionSheetAsync("실시간 카카오스토리 알림", Constants.PromptCancel, null, OnText, OffText);
        if (action == null || action == Constants.PromptCancel) return;

        var isEnabled = action == OnText;
        if (isEnabled)
        {
            // The ongoing notification is required to keep the service alive;
            // confirm it before enabling so the user is not surprised.
            var confirmed = await DisplayAlertAsync("안내", "실시간 알림을 사용하면 알림 표시줄에 상시 알림이 표시됩니다. 계속하시겠습니까?", Constants.PromptOk, Constants.PromptCancel);
            if (!confirmed) return;
        }

        Configuration.SetValue(KakaoStoryRealtimeNotificationManager.EnabledKey, isEnabled);
        KakaoStoryRealtimeNotificationLabel.Text = isEnabled ? OnText : OffText;

        if (isEnabled) KakaoStoryRealtimeNotificationManager.StartIfEnabled();
        else KakaoStoryRealtimeNotificationManager.Stop();
    }
#else
    private void OnKakaoStoryRealtimeNotificationGridTapped(object sender, TappedEventArgs e) { }
#endif

    private async void OnKakaoStoryForegroundPollIntervalGridTapped(object sender, TappedEventArgs e)
    {
        var options = new[] { "1초", "3초", "5초", "10초", "20초", "30초", "40초", "1분" };
        var action = await DisplayActionSheetAsync("앱 사용중 폴링 주기", Constants.PromptCancel, null, options);
        if (action == null || action == Constants.PromptCancel) return;

        var seconds = ParsePollInterval(action);
        Configuration.SetValue(KakaoStoryNotificationPoller.ForegroundPollIntervalSecondsKey, seconds);
        KakaoStoryForegroundPollIntervalLabel.Text = action;

        // Restart the loop so the new interval applies immediately; the loop
        // re-evaluates the period every cycle, but a restart also covers the
        // case where the loop is not running yet.
        if (KakaoStoryNotificationPoller.IsForegroundPollingRunning)
        {
            KakaoStoryNotificationPoller.StopForegroundPolling();
            KakaoStoryNotificationPoller.StartForegroundPolling();
        }
    }

#if ANDROID
    private async void OnKakaoStoryRealtimeScreenOnPollIntervalGridTapped(object sender, TappedEventArgs e)
    {
        var options = new[] { "5초", "10초", "20초", "30초", "40초", "1분", "2분", "5분" };
        var action = await DisplayActionSheetAsync("실시간 알림 (화면 켜짐) 폴링 주기", Constants.PromptCancel, null, options);
        if (action == null || action == Constants.PromptCancel) return;

        Configuration.SetValue(KakaoStoryRealtimeNotificationService.ScreenOnPollIntervalSecondsKey, ParsePollInterval(action));
        KakaoStoryRealtimeScreenOnPollIntervalLabel.Text = action;
    }

    private async void OnKakaoStoryRealtimeScreenOffPollIntervalGridTapped(object sender, TappedEventArgs e)
    {
        var options = new[] { "1분", "2분", "3분", "5분", "10분" };
        var action = await DisplayActionSheetAsync("실시간 알림 (화면 꺼짐) 폴링 주기", Constants.PromptCancel, null, options);
        if (action == null || action == Constants.PromptCancel) return;

        Configuration.SetValue(KakaoStoryRealtimeNotificationService.ScreenOffPollIntervalSecondsKey, ParsePollInterval(action));
        KakaoStoryRealtimeScreenOffPollIntervalLabel.Text = action;
    }
#else
    private void OnKakaoStoryRealtimeScreenOnPollIntervalGridTapped(object sender, TappedEventArgs e) { }
    private void OnKakaoStoryRealtimeScreenOffPollIntervalGridTapped(object sender, TappedEventArgs e) { }
#endif

    private static double ParsePollInterval(string text)
    {
        var value = double.Parse(text[..^1]);
        return text.EndsWith("분") ? value * 60 : value;
    }

    private async void OnKakaoStoryFavoriteFriendNotificationGridTapped(object sender, TappedEventArgs e)
    {
        var action = await DisplayActionSheetAsync("카카오스토리 관심 친구 푸시 알림", Constants.PromptCancel, null, OnText, OffText);
        if (action == null || action == Constants.PromptCancel) return;

        var isEnabled = action == OnText;
        Configuration.SetValue("KakaoStoryFavoriteFriendNotificationEnabled", isEnabled);
        KakaoStoryFavoriteFriendNotificationLabel.Text = isEnabled ? OnText : OffText;
    }

    private async void OnKakaoStoryEmotionNotificationGridTapped(object sender, TappedEventArgs e)
    {
        var action = await DisplayActionSheetAsync("카카오스토리 느낌 푸시 알림", Constants.PromptCancel, null, OnText, OffText);
        if (action == null || action == Constants.PromptCancel) return;

        var isEnabled = action == OnText;
        Configuration.SetValue("KakaoStoryEmotionNotificationEnabled", isEnabled);
        KakaoStoryEmotionNotificationLabel.Text = isEnabled ? OnText : OffText;
    }

    private async void OnKakaoStoryMailNotificationGridTapped(object sender, TappedEventArgs e)
    {
        var action = await DisplayActionSheetAsync("카카오스토리 쪽지 푸시 알림", Constants.PromptCancel, null, OnText, OffText);
        if (action == null || action == Constants.PromptCancel) return;

        var isEnabled = action == OnText;
        Configuration.SetValue("KakaoStoryMailNotificationEnabled", isEnabled);
        KakaoStoryMailNotificationLabel.Text = isEnabled ? OnText : OffText;
    }

    private async void OnKakaoStorySessionExpiredNotificationGridTapped(object sender, TappedEventArgs e)
    {
        var action = await DisplayActionSheetAsync("카카오스토리 로그인 만료 푸시 알림", Constants.PromptCancel, null, OnText, OffText);
        if (action == null || action == Constants.PromptCancel) return;

        var isEnabled = action == OnText;
        Configuration.SetValue("KakaoStorySessionExpiredNotificationEnabled", isEnabled);
        KakaoStorySessionExpiredNotificationLabel.Text = isEnabled ? OnText : OffText;
    }

    private async void OnKakaoStoryNotificationBadgeGridTapped(object sender, TappedEventArgs e)
    {
        var action = await DisplayActionSheetAsync("알림 배지 합산", Constants.PromptCancel, null, OnText, OffText);
        if (action == null || action == Constants.PromptCancel) return;

        var isEnabled = action == OnText;
        Configuration.SetValue("KakaoStoryNotificationBadgeEnabled", isEnabled);
        KakaoStoryNotificationBadgeLabel.Text = isEnabled ? OnText : OffText;

        // Re-render the badge immediately; when enabled, poll once so the count refreshes right away.
        Shared.RefreshTabBadges();
        if (isEnabled) _ = TabBarBadgePoller.PollOnceAsync();
    }

    private async void OnKakaoStoryMailBadgeGridTapped(object sender, TappedEventArgs e)
    {
        var action = await DisplayActionSheetAsync("쪽지 배지 합산", Constants.PromptCancel, null, OnText, OffText);
        if (action == null || action == Constants.PromptCancel) return;

        var isEnabled = action == OnText;
        Configuration.SetValue("KakaoStoryMailBadgeEnabled", isEnabled);
        KakaoStoryMailBadgeLabel.Text = isEnabled ? OnText : OffText;

        // Re-render the badge immediately; when enabled, poll once so the count refreshes right away.
        Shared.RefreshTabBadges();
        if (isEnabled) _ = TabBarBadgePoller.PollOnceAsync();
    }

    private async void OnKakaoStoryFriendRequestBadgeGridTapped(object sender, TappedEventArgs e)
    {
        var action = await DisplayActionSheetAsync("친구 신청 배지 합산", Constants.PromptCancel, null, OnText, OffText);
        if (action == null || action == Constants.PromptCancel) return;

        var isEnabled = action == OnText;
        Configuration.SetValue("KakaoStoryFriendRequestBadgeEnabled", isEnabled);
        KakaoStoryFriendRequestBadgeLabel.Text = isEnabled ? OnText : OffText;

        // Re-render the badge immediately; when enabled, poll once so the count refreshes right away.
        Shared.RefreshTabBadges();
        if (isEnabled) _ = TabBarBadgePoller.PollOnceAsync();
    }

    private async void OnCheckForUpdateGridTapped(object sender, TappedEventArgs e) => await Utils.CheckForUpdateAsync();

    private async void OnCommentPushNotificationPermissionGridTapped(object sender, TappedEventArgs e) => await SetupPushNotificationPermission(PushNotificationType.Comment);
    private async void OnCommentMentionPushNotificationPermissionGridTapped(object sender, TappedEventArgs e) => await SetupPushNotificationPermission(PushNotificationType.CommentMention);
    private async void OnCommentLikePushNotificationPermissionGridTapped(object sender, TappedEventArgs e) => await SetupPushNotificationPermission(PushNotificationType.CommentLike);
    private async void OnSharedPostCommentPushNotificationPermissionGridTapped(object sender, TappedEventArgs e) => await SetupPushNotificationPermission(PushNotificationType.SharedPostComment);
    private async void OnPostReactionPushNotificationPermissionGridTapped(object sender, TappedEventArgs e) => await SetupPushNotificationPermission(PushNotificationType.PostReaction);
    private async void OnPostMentionPushNotificationPermissionGridTapped(object sender, TappedEventArgs e) => await SetupPushNotificationPermission(PushNotificationType.PostMention);
    private async void OnIsFavoriteFriendNewPostPushNotificationEnabledGridTapped(object sender, TappedEventArgs e) 
    {
        var action = await DisplayActionSheetAsync("관심 친구 새 글 푸시 알림", Constants.PromptCancel, null, OnText, OffText);
        if (action == null || action == Constants.PromptCancel) return;

        var isEnabled = action == OnText;
        var result = await App.ExecuteRequestAsync(new UpdatePushNotificationPermission(PushNotificationType.FavoriteFriendNewPost, isEnabled ? AccessPermission.Everyone : AccessPermission.OnlyMe));
        if (result.IsSuccess)
        {
            IsFavoriteFriendNewPostPushNotificationEnabledLabel.Text = isEnabled ? OnText : OffText;
            _user.IsFavoriteFriendNewPostPushNotificationEnabled = isEnabled;
            await DisplayAlertAsync("안내", $"관심 친구 새 글 푸시 알림이 {action}으로 설정되었습니다.", Constants.PromptOk);
        }
    }

    private async void OnThemeGridTapped(object sender, TappedEventArgs e)
    {
        var action = await DisplayActionSheetAsync("테마 설정", Constants.PromptCancel, null, "시스템 설정 따름", "라이트 모드", "다크 모드");
        if (action == null || action == Constants.PromptCancel) return;

        string themeValue = null;
        AppTheme appTheme = AppTheme.Unspecified;

        if (action == "라이트 모드")
        {
            themeValue = "Light";
            appTheme = AppTheme.Light;
        }
        else if (action == "다크 모드")
        {
            themeValue = "Dark";
            appTheme = AppTheme.Dark;
        }

        Configuration.SetValue("Theme", themeValue);
        Application.Current.UserAppTheme = appTheme;
        ThemeLabel.Text = action;

        await DisplayAlertAsync("안내", "테마 변경을 적용하려면 앱을 다시 시작해야 합니다.", Constants.PromptOk);
        Configuration.WriteBuffer();
        Environment.Exit(0);
    }

#if ANDROID
    private async void OnTimelineVirtualizationGridTapped(object sender, TappedEventArgs e)
    {
        var action = await DisplayActionSheetAsync("타임라인 스크롤 가상화", Constants.PromptCancel, null, OnText, OffText);
        if (action == null || action == Constants.PromptCancel) return;

        var isEnabled = action == OnText;
        Configuration.SetValue("TimelineVirtualizationEnabled", isEnabled);
        TimelineVirtualizationLabel.Text = isEnabled ? OnText : OffText;

        WeakReferenceMessenger.Default.Send(new TimelineVirtualizationChangedMessage(isEnabled));
    }
#else
    private void OnTimelineVirtualizationGridTapped(object sender, TappedEventArgs e) { }
#endif
}