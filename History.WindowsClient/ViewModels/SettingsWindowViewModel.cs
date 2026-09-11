using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using History.Commons;
using History.Commons.Api.User;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using History.Commons.KakaoStory;
using History.WindowsClient.Helpers;
using History.WindowsClient.Messages;
using History.WindowsClient.Models;
using History.WindowsClient.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.System;

namespace History.WindowsClient.ViewModels;

// Settings window state. The dialog, picker, and loading events are fulfilled by the
// settings window code-behind; the profile-backed values (birthday, friend-list range,
// push permissions) are loaded from the server on window load. Single-choice rows use
// inline combo boxes so the displayed value always matches the stored setting.
public sealed partial class SettingsWindowViewModel : BaseViewModel
{
    private const string TermsUrl = "https://history.cenox.io/terms.html";
    private const string PrivacyPolicyUrl = "https://history.cenox.io/privacypolicy.html";
    private const string OffText = "꺼짐";
    private const string NotSetText = "설정되지 않음";
    private const string WithdrawConfirmationText = "탈퇴하겠습니다";

    private const string KakaoStoryNotificationEnabledKey = "KakaoStoryNotificationEnabled";
    private const string KakaoStoryFavoriteFriendNotificationEnabledKey = "KakaoStoryFavoriteFriendNotificationEnabled";
    private const string KakaoStoryEmotionNotificationEnabledKey = "KakaoStoryEmotionNotificationEnabled";
    private const string KakaoStoryProfanityCheckEnabledKey = "KakaoStoryProfanityCheckEnabled";

    private static readonly DiscoveryOption[] s_friendListDiscoveryOptions = [DiscoveryOption.OnlyMe, DiscoveryOption.Friends, DiscoveryOption.FriendsOfFriends, DiscoveryOption.Everyone];

    private const int VersionUnlockTapCount = 6;
    private static readonly TimeSpan VersionUnlockTapInterval = TimeSpan.FromSeconds(3);

    private readonly ApplicationSettingsService _settingsService;
    private readonly ApplicationThemeService _applicationThemeService;
    private readonly StoreUpdateService _storeUpdateService;
    private readonly PushNotificationService _pushNotificationService;
    private readonly ApplicationSettings _settings;

    private UserResponseDto _profile;
    private bool _suppressChangeHandlers = true;
    private int _versionTapCount;
    private DateTime? _lastVersionTapTime;

    public SettingsWindowViewModel(ApplicationSettingsService settingsService, ApplicationThemeService applicationThemeService, StoreUpdateService storeUpdateService, PushNotificationService pushNotificationService)
    {
        _settingsService = settingsService;
        _applicationThemeService = applicationThemeService;
        _storeUpdateService = storeUpdateService;
        _pushNotificationService = pushNotificationService;
        _settings = settingsService.Settings;

        IsOnlyMePostContinuationPromptEnabled = _settings.IsOnlyMePostContinuationPromptEnabled;
        IsAutomaticUpdateCheckEnabled = _settings.IsAutomaticUpdateCheckEnabled;
        IsKakaoStoryNotificationEnabled = Configuration.GetValue<bool?>(KakaoStoryNotificationEnabledKey) ?? true;
        IsKakaoStoryFavoriteFriendNotificationEnabled = Configuration.GetValue<bool?>(KakaoStoryFavoriteFriendNotificationEnabledKey) ?? true;
        IsKakaoStoryEmotionNotificationEnabled = Configuration.GetValue<bool?>(KakaoStoryEmotionNotificationEnabledKey) ?? true;
        IsKakaoStoryProfanityCheckEnabled = Configuration.GetValue<bool?>(KakaoStoryProfanityCheckEnabledKey) ?? true;
        ThemeIndex = _settings.Theme switch { ElementTheme.Light => 1, ElementTheme.Dark => 2, _ => 0 };
        KakaoStorySectionVisibility = KakaoStoryFeatureGateHelper.IsEnabled ? Visibility.Visible : Visibility.Collapsed;

        _suppressChangeHandlers = false;
    }

    // Raised when the window should close itself (for example after sign-out).
    public event EventHandler CloseRequested;

    // Fulfilled by the settings window code-behind, which hosts the Kakao login window as a
    // modal over this settings window.
    public event Func<Task> KakaoReloginRequested;

    public string VersionText => App.GetApplicationVersion();

    // Hidden unlock: the section stays collapsed until the version row is tapped enough times.
    [ObservableProperty]
    public partial Visibility KakaoStorySectionVisibility { get; private set; }

    public string BirthdayText
    {
        get
        {
            var parts = _profile?.Birthday?.Split('-') ?? [];
            return parts.Length == 2 ? $"{parts[0]}월 {parts[1]}일" : NotSetText;
        }
    }

    // Index 0..3 maps to OnlyMe/Friends/FriendsOfFriends/Everyone (the access permission values).
    public string[] PushPermissionOptions { get; } = [.. Enum.GetValues<AccessPermission>().Select(permission => permission == AccessPermission.OnlyMe ? OffText : permission.ToDisplayString())];

    public string[] FriendListDiscoveryOptions { get; } = [.. s_friendListDiscoveryOptions.Select(option => option.ToDisplayString())];

    // 0 = 시스템 설정 따름, 1 = 라이트 모드, 2 = 다크 모드. The combo box selection is the
    // single source of truth, so the displayed value always matches the applied theme.
    [ObservableProperty]
    public partial int ThemeIndex { get; set; }

    partial void OnThemeIndexChanged(int value)
    {
        if (_suppressChangeHandlers) return;
        var theme = value switch { 1 => ElementTheme.Light, 2 => ElementTheme.Dark, _ => ElementTheme.Default };
        _applicationThemeService.SetTheme(theme);
    }

    [ObservableProperty]
    public partial int FriendListDiscoveryIndex { get; set; }
    partial void OnFriendListDiscoveryIndexChanged(int value) => _ = ApplyFriendListDiscoveryOptionAsync(value);

    [ObservableProperty]
    public partial int CommentPushNotificationIndex { get; set; }
    partial void OnCommentPushNotificationIndexChanged(int value) => _ = ApplyPushNotificationPermissionAsync(PushNotificationType.Comment, value);

    [ObservableProperty]
    public partial int CommentMentionPushNotificationIndex { get; set; }
    partial void OnCommentMentionPushNotificationIndexChanged(int value) => _ = ApplyPushNotificationPermissionAsync(PushNotificationType.CommentMention, value);

    [ObservableProperty]
    public partial int CommentLikePushNotificationIndex { get; set; }
    partial void OnCommentLikePushNotificationIndexChanged(int value) => _ = ApplyPushNotificationPermissionAsync(PushNotificationType.CommentLike, value);

    [ObservableProperty]
    public partial int SharedPostCommentPushNotificationIndex { get; set; }
    partial void OnSharedPostCommentPushNotificationIndexChanged(int value) => _ = ApplyPushNotificationPermissionAsync(PushNotificationType.SharedPostComment, value);

    [ObservableProperty]
    public partial int PostReactionPushNotificationIndex { get; set; }
    partial void OnPostReactionPushNotificationIndexChanged(int value) => _ = ApplyPushNotificationPermissionAsync(PushNotificationType.PostReaction, value);

    [ObservableProperty]
    public partial int PostMentionPushNotificationIndex { get; set; }
    partial void OnPostMentionPushNotificationIndexChanged(int value) => _ = ApplyPushNotificationPermissionAsync(PushNotificationType.PostMention, value);

    [ObservableProperty]
    public partial bool IsOnlyMePostContinuationPromptEnabled { get; set; }
    partial void OnIsOnlyMePostContinuationPromptEnabledChanged(bool value) => _settings.IsOnlyMePostContinuationPromptEnabled = value;

    [ObservableProperty]
    public partial bool IsAutomaticUpdateCheckEnabled { get; set; }
    partial void OnIsAutomaticUpdateCheckEnabledChanged(bool value) => _settings.IsAutomaticUpdateCheckEnabled = value;

    [ObservableProperty]
    public partial bool IsFavoriteFriendNewPostPushNotificationEnabled { get; set; }
    partial void OnIsFavoriteFriendNewPostPushNotificationEnabledChanged(bool value)
    {
        if (_suppressChangeHandlers) return;
        _ = ChangeFavoriteFriendNewPostPushNotificationAsync(value);
    }

    [ObservableProperty]
    public partial bool IsKakaoStoryNotificationEnabled { get; set; }
    partial void OnIsKakaoStoryNotificationEnabledChanged(bool value)
    {
        if (_suppressChangeHandlers) return;
        Configuration.SetValue(KakaoStoryNotificationEnabledKey, value);
        _ = ExecuteWithLoadingAsync(() => value ? CommonKakaoStoryUtils.UploadTokenToServerAsync() : CommonKakaoStoryUtils.DeleteTokenFromServerAsync());
    }

    [ObservableProperty]
    public partial bool IsKakaoStoryFavoriteFriendNotificationEnabled { get; set; }
    partial void OnIsKakaoStoryFavoriteFriendNotificationEnabledChanged(bool value)
    {
        if (_suppressChangeHandlers) return;
        Configuration.SetValue(KakaoStoryFavoriteFriendNotificationEnabledKey, value);
        _ = ExecuteWithLoadingAsync(CommonKakaoStoryUtils.UploadTokenToServerAsync);
    }

    [ObservableProperty]
    public partial bool IsKakaoStoryEmotionNotificationEnabled { get; set; }
    partial void OnIsKakaoStoryEmotionNotificationEnabledChanged(bool value)
    {
        if (_suppressChangeHandlers) return;
        Configuration.SetValue(KakaoStoryEmotionNotificationEnabledKey, value);
        _ = ExecuteWithLoadingAsync(CommonKakaoStoryUtils.UploadTokenToServerAsync);
    }

    // Persisted for the Kakao Story upload flow, which reads it before mirroring the text.
    [ObservableProperty]
    public partial bool IsKakaoStoryProfanityCheckEnabled { get; set; }

    partial void OnIsKakaoStoryProfanityCheckEnabledChanged(bool value)
    {
        if (_suppressChangeHandlers) return;
        Configuration.SetValue(KakaoStoryProfanityCheckEnabledKey, value);
    }

    public async Task LoadAsync()
    {
        var profileResult = await ExecuteRequestAsync(new GetMyProfile());
        if (profileResult.IsFailure) return;

        _profile = profileResult.Value;

        _suppressChangeHandlers = true;
        IsFavoriteFriendNewPostPushNotificationEnabled = _profile.IsFavoriteFriendNewPostPushNotificationEnabled;
        FriendListDiscoveryIndex = Math.Max(Array.IndexOf(s_friendListDiscoveryOptions, _profile.FriendListDiscoveryOption), 0);
        CommentPushNotificationIndex = (int)_profile.CommentPushNotificationPermission;
        CommentMentionPushNotificationIndex = (int)_profile.CommentMentionPushNotificationPermission;
        CommentLikePushNotificationIndex = (int)_profile.CommentLikePushNotificationPermission;
        SharedPostCommentPushNotificationIndex = (int)_profile.SharedPostCommentPushNotificationPermission;
        PostReactionPushNotificationIndex = (int)_profile.PostReactionPushNotificationPermission;
        PostMentionPushNotificationIndex = (int)_profile.PostMentionPushNotificationPermission;
        _suppressChangeHandlers = false;

        OnPropertyChanged(nameof(BirthdayText));
    }

    [RelayCommand]
    private async Task SetBirthdayAsync()
    {
        var hasBirthday = TryParseBirthday(_profile?.Birthday, out var currentBirthday);
        var datePicker = new DatePicker
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            MinYear = new DateTimeOffset(new DateTime(1900, 1, 1)),
            MaxYear = new DateTimeOffset(new DateTime(DateTime.Now.Year, 12, 31)),
            Date = hasBirthday ? new DateTimeOffset(new DateTime(DateTime.Now.Year, currentBirthday.Month, currentBirthday.Day)) : DateTimeOffset.Now,
        };

        var dialog = new ContentDialog
        {
            Title = "생일 설정",
            PrimaryButtonText = "확인",
            CloseButtonText = "취소",
            DefaultButton = ContentDialogButton.Primary,
            Content = datePicker,
        };
        if (hasBirthday) dialog.SecondaryButtonText = "생일 삭제";

        var result = await ShowContentDialogAsync(dialog);
        if (result == ContentDialogResult.Secondary) await UpdateBirthdayAsync(null);
        else if (result == ContentDialogResult.Primary) await UpdateBirthdayAsync(new DateTime(DateTime.Now.Year, datePicker.Date.Month, datePicker.Date.Day));
    }

    [RelayCommand]
    private async Task CheckForUpdateAsync()
    {
        int availableUpdateCount;
        try { availableUpdateCount = await ExecuteWithLoadingAsync(() => _storeUpdateService.GetAvailableUpdateCountAsync()); }
        catch
        {
            await ShowMessageDialogAsync(new MessageDialogParameters(Constants.ErrorTitle, "업데이트 확인에 실패하였습니다."));
            return;
        }

        if (availableUpdateCount <= 0)
        {
            await ShowMessageDialogAsync(new MessageDialogParameters("업데이트 확인", "최신 버전을 사용하고 있습니다."));
            return;
        }

        var openResult = await ShowMessageDialogAsync(new MessageDialogParameters("업데이트 확인", $"{availableUpdateCount}개의 업데이트가 있습니다. Microsoft Store에서 업데이트하시겠습니까?", "스토어 열기", "나중에"));
        if (openResult == ContentDialogResult.Primary) await _storeUpdateService.OpenStoreProductPageAsync();
    }

    // Hidden unlock: the version row must be tapped six times with no more than a three-second
    // gap between taps. The confirmation warns that the Kakao Story integration is not an
    // official Kakao feature before it is enabled.
    [RelayCommand]
    private async Task HandleVersionInfoAsync()
    {
        var now = DateTime.Now;
        if (_lastVersionTapTime == null || (now - _lastVersionTapTime.Value) > VersionUnlockTapInterval) _versionTapCount = 1;
        else _versionTapCount++;

        _lastVersionTapTime = now;

        if (_versionTapCount < VersionUnlockTapCount) return;

        _versionTapCount = 0;
        _lastVersionTapTime = null;

        if (KakaoStoryFeatureGateHelper.IsEnabled) return;

        var confirmation = await ShowMessageDialogAsync(new MessageDialogParameters("카카오스토리 연동 기능 활성화", "카카오스토리 연동 기능은 카카오스토리의 공식 기능이 아닙니다. 카카오스토리 측에서 제재할 수 있는 가능성이 있습니다. 이 기능을 활성화하시겠습니까?", "활성화", "취소"));
        if (confirmation != ContentDialogResult.Primary) return;

        KakaoStoryFeatureGateHelper.Enable();
        KakaoStorySectionVisibility = Visibility.Visible;
        WeakReferenceMessenger.Default.Send(new KakaoStoryFeaturesEnabledMessage());
    }

    [RelayCommand]
    private async Task OpenTermsAsync() => await Launcher.LaunchUriAsync(new Uri(TermsUrl));

    [RelayCommand]
    private async Task OpenPrivacyPolicyAsync() => await Launcher.LaunchUriAsync(new Uri(PrivacyPolicyUrl));

    [RelayCommand]
    private async Task LogoutAsync()
    {
        var confirmation = await ShowMessageDialogAsync(new MessageDialogParameters("안내", "정말로 로그아웃을 하시겠습니까?", "네", "아니오"));
        if (confirmation != ContentDialogResult.Primary) return;

        await ExecuteWithLoadingAsync(async () =>
        {
            await CommonKakaoStoryUtils.DeleteTokenFromServerAsync();

            // Revoke the current refresh token on the server so the session cannot be resumed.
            var refreshToken = _settings.RefreshToken;
            if (!string.IsNullOrEmpty(refreshToken)) await CommonShared.ApiHandler.TryExecuteRequestAsync(new Logout(refreshToken));

            await _pushNotificationService.UnregisterAsync();
        });

        ClearSharedState();
        WeakReferenceMessenger.Default.Send(new LogoutRequestedMessage());
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private async Task WithdrawAsync()
    {
        var firstConfirmation = await ShowMessageDialogAsync(new MessageDialogParameters("안내", "정말로 회원 탈퇴를 하시겠습니까?", "네", "아니오"));
        if (firstConfirmation != ContentDialogResult.Primary) return;

        var secondConfirmation = await ShowMessageDialogAsync(new MessageDialogParameters("경고", "회원 탈퇴는 되돌릴 수 없습니다. 정말로 회원 탈퇴를 하시겠습니까?", "네", "아니오"));
        if (secondConfirmation != ContentDialogResult.Primary) return;

        var prompt = await ShowInputDialogAsync(new InputDialogParameters("경고", $"회원 탈퇴를 하시면 이용 약관에 따라 유예 기간 없이 모든 데이터가 삭제됩니다. 이에 동의하시면 아래 \"{WithdrawConfirmationText}\"를 따옴표 없이 입력해주세요.", WithdrawConfirmationText, showCancel: true));
        if (prompt != WithdrawConfirmationText) return;

        var withdrawResult = await ExecuteRequestAsync(new Withdraw());
        if (withdrawResult.IsFailure) return;

        await ExecuteWithLoadingAsync(async () =>
        {
            await CommonKakaoStoryUtils.DeleteTokenFromServerAsync();
            await _pushNotificationService.UnregisterAsync();
        });

        ClearSharedState();
        await ShowMessageDialogAsync(new MessageDialogParameters("안내", "회원 탈퇴가 완료되었습니다. 이용해 주셔서 감사합니다."));
        WeakReferenceMessenger.Default.Send(new LogoutRequestedMessage());
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private async Task KakaoReloginAsync()
    {
        if (KakaoReloginRequested == null) return;
        await KakaoReloginRequested.Invoke();
    }

    [RelayCommand]
    private async Task ResetKakaoCredentialsAsync()
    {
        var savedEmail = await KakaoStoryCredentialStore.GetEmailAsync();
        if (string.IsNullOrEmpty(savedEmail))
        {
            await ShowMessageDialogAsync(new MessageDialogParameters("안내", "저장된 카카오스토리 로그인 정보가 없습니다."));
            return;
        }

        var confirmation = await ShowMessageDialogAsync(new MessageDialogParameters("확인", "저장된 카카오스토리 로그인 정보를 초기화하시겠습니까?", "확인", "취소"));
        if (confirmation != ContentDialogResult.Primary) return;

        KakaoStoryCredentialStore.Clear();
        KakaoStoryApiHandler.ClearSdkTokens();
        await CommonKakaoStoryUtils.DeleteTokenFromServerAsync();
        await ShowMessageDialogAsync(new MessageDialogParameters("안내", "카카오스토리 로그인 정보가 초기화되었습니다."));
    }

    private async Task ApplyFriendListDiscoveryOptionAsync(int index)
    {
        if (_suppressChangeHandlers) return;
        if (index < 0 || index >= s_friendListDiscoveryOptions.Length) return;

        var discoveryOption = s_friendListDiscoveryOptions[index];
        var result = await ExecuteRequestAsync(new UpdateFriendListDiscoveryOption(discoveryOption));
        if (result.IsSuccess)
        {
            if (_profile != null) _profile.FriendListDiscoveryOption = discoveryOption;
            return;
        }

        if (_profile == null) return;

        // The update failed: restore the selection to the value the server still holds.
        _suppressChangeHandlers = true;
        FriendListDiscoveryIndex = Math.Max(Array.IndexOf(s_friendListDiscoveryOptions, _profile.FriendListDiscoveryOption), 0);
        _suppressChangeHandlers = false;
    }

    private async Task ApplyPushNotificationPermissionAsync(PushNotificationType type, int index)
    {
        if (_suppressChangeHandlers) return;
        if (index < 0 || index >= PushPermissionOptions.Length) return;

        var permission = (AccessPermission)index;
        var result = await ExecuteRequestAsync(new UpdatePushNotificationPermission(type, permission));
        if (result.IsSuccess)
        {
            SetProfilePushNotificationPermission(type, permission);
            return;
        }

        if (_profile == null) return;

        // The update failed: restore the selection to the value the server still holds.
        _suppressChangeHandlers = true;
        SetPushNotificationIndex(type, (int)GetProfilePushNotificationPermission(type));
        _suppressChangeHandlers = false;
    }

    private async Task UpdateBirthdayAsync(DateTime? birthday)
    {
        var result = await ExecuteRequestAsync(new UpdateBirthday(birthday));
        if (result.IsFailure) return;

        if (_profile != null) _profile.Birthday = birthday is { } value ? $"{value.Month:D2}-{value.Day:D2}" : null;
        OnPropertyChanged(nameof(BirthdayText));
    }

    private async Task ChangeFavoriteFriendNewPostPushNotificationAsync(bool isEnabled)
    {
        var result = await ExecuteRequestAsync(new UpdatePushNotificationPermission(PushNotificationType.FavoriteFriendNewPost, isEnabled ? AccessPermission.Everyone : AccessPermission.OnlyMe));
        if (result.IsSuccess)
        {
            if (_profile != null) _profile.IsFavoriteFriendNewPostPushNotificationEnabled = isEnabled;
            return;
        }

        if (_profile == null) return;

        // The update failed: restore the toggle to the value the server still holds.
        _suppressChangeHandlers = true;
        IsFavoriteFriendNewPostPushNotificationEnabled = _profile.IsFavoriteFriendNewPostPushNotificationEnabled;
        _suppressChangeHandlers = false;
    }

    private void SetProfilePushNotificationPermission(PushNotificationType type, AccessPermission permission)
    {
        if (_profile == null) return;

        switch (type)
        {
            case PushNotificationType.Comment: _profile.CommentPushNotificationPermission = permission; break;
            case PushNotificationType.CommentMention: _profile.CommentMentionPushNotificationPermission = permission; break;
            case PushNotificationType.CommentLike: _profile.CommentLikePushNotificationPermission = permission; break;
            case PushNotificationType.SharedPostComment: _profile.SharedPostCommentPushNotificationPermission = permission; break;
            case PushNotificationType.PostReaction: _profile.PostReactionPushNotificationPermission = permission; break;
            case PushNotificationType.PostMention: _profile.PostMentionPushNotificationPermission = permission; break;
        }
    }

    private AccessPermission GetProfilePushNotificationPermission(PushNotificationType type) => type switch
    {
        PushNotificationType.Comment => _profile.CommentPushNotificationPermission,
        PushNotificationType.CommentMention => _profile.CommentMentionPushNotificationPermission,
        PushNotificationType.CommentLike => _profile.CommentLikePushNotificationPermission,
        PushNotificationType.SharedPostComment => _profile.SharedPostCommentPushNotificationPermission,
        PushNotificationType.PostReaction => _profile.PostReactionPushNotificationPermission,
        PushNotificationType.PostMention => _profile.PostMentionPushNotificationPermission,
        _ => AccessPermission.OnlyMe,
    };

    private void SetPushNotificationIndex(PushNotificationType type, int index)
    {
        switch (type)
        {
            case PushNotificationType.Comment: CommentPushNotificationIndex = index; break;
            case PushNotificationType.CommentMention: CommentMentionPushNotificationIndex = index; break;
            case PushNotificationType.CommentLike: CommentLikePushNotificationIndex = index; break;
            case PushNotificationType.SharedPostComment: SharedPostCommentPushNotificationIndex = index; break;
            case PushNotificationType.PostReaction: PostReactionPushNotificationIndex = index; break;
            case PushNotificationType.PostMention: PostMentionPushNotificationIndex = index; break;
        }
    }

    private void ClearSharedState()
    {
        _settings.AccessToken = null;
        _settings.RefreshToken = null;
        _settingsService.SaveSettings();

        CommonShared.ApiHandler = ApiHandler.Public;
        CommonShared.UserId = default;
        CommonShared.MyRank = default;
        CommonShared.LastUsedPostDiscoveryOption = default;
        CommonShared.Friends = default;
        CommonShared.KakaoFriends = default;
        CommonShared.KakaoUserId = default;
        CommonShared.KakaoProfileImageUrl = default;
    }

    private static bool TryParseBirthday(string birthday, out DateTime parsedBirthday)
    {
        parsedBirthday = default;
        if (string.IsNullOrEmpty(birthday)) return false;

        var parts = birthday.Split('-');
        if (parts.Length != 2) return false;
        if (!int.TryParse(parts[0], out var month) || !int.TryParse(parts[1], out var day)) return false;

        try { parsedBirthday = new DateTime(DateTime.Now.Year, month, day); }
        catch { return false; }

        return true;
    }
}
