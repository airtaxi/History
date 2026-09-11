using Microsoft.UI.Xaml;
using History.WindowsClient.Models;
using Windows.Storage;
using System.ComponentModel;

namespace History.WindowsClient.Services;

public sealed partial class ApplicationSettingsService : IDisposable
{
    private const string ThemeSettingKey = "Theme";
    private const string IsAutomaticUpdateCheckEnabledSettingKey = "IsAutomaticUpdateCheckEnabled";
    private const string AccessTokenSettingKey = "AccessToken";
    private const string RefreshTokenSettingKey = "RefreshToken";
    private const string IsKakaoPostEnabledSettingKey = "IsKakaoPostEnabled";
    private const string IsTimelineRefreshEnabledOnNewPostSettingKey = "IsTimelineRefreshEnabledOnNewPost";
    private const string IsTimelineRefreshEnabledOnNewShareSettingKey = "IsTimelineRefreshEnabledOnNewShare";
    private const string IsOnlyMePostContinuationPromptEnabledSettingKey = "IsOnlyMePostContinuationPromptEnabled";

    private bool _disposed;

    public bool AutoSave { get; set; } = true;

    public ApplicationSettingsService()
    {
        Settings = LoadSettings();

        Settings.PropertyChanged += OnApplicationSettingsPropertyChanged;
    }

    public event EventHandler SettingsChanged;

    public ApplicationSettings Settings { get; }

    public void SaveSettings()
    {
        NormalizeSettings(Settings);
        var localSettings = ApplicationData.Current.LocalSettings;
        localSettings.Values[ThemeSettingKey] = Settings.Theme.ToString();
        localSettings.Values[IsAutomaticUpdateCheckEnabledSettingKey] = Settings.IsAutomaticUpdateCheckEnabled;
        localSettings.Values[AccessTokenSettingKey] = Settings.AccessToken;
        localSettings.Values[RefreshTokenSettingKey] = Settings.RefreshToken;
        localSettings.Values[IsKakaoPostEnabledSettingKey] = Settings.IsKakaoPostEnabled;
        localSettings.Values[IsTimelineRefreshEnabledOnNewPostSettingKey] = Settings.IsTimelineRefreshEnabledOnNewPost;
        localSettings.Values[IsTimelineRefreshEnabledOnNewShareSettingKey] = Settings.IsTimelineRefreshEnabledOnNewShare;
        localSettings.Values[IsOnlyMePostContinuationPromptEnabledSettingKey] = Settings.IsOnlyMePostContinuationPromptEnabled;
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    private static ApplicationSettings LoadSettings()
    {
        var localSettings = ApplicationData.Current.LocalSettings;

        // Fetch values from local settings
        var theme = ElementTheme.Default;
        if (localSettings.Values.TryGetValue(ThemeSettingKey, out var storedTheme) && storedTheme is string themeString && Enum.TryParse(themeString, true, out theme)) { }

        var isAutomaticUpdateCheckEnabled = true;
        if (localSettings.Values.TryGetValue(IsAutomaticUpdateCheckEnabledSettingKey, out var storedAutoCheck) && storedAutoCheck is bool autoCheckValue) isAutomaticUpdateCheckEnabled = autoCheckValue;

        string accessToken = null;
        if (localSettings.Values.TryGetValue(AccessTokenSettingKey, out var storedAccessToken) && storedAccessToken is string accessTokenValue) accessToken = accessTokenValue;

        string refreshToken = null;
        if (localSettings.Values.TryGetValue(RefreshTokenSettingKey, out var storedRefreshToken) && storedRefreshToken is string refreshTokenValue) refreshToken = refreshTokenValue;

        var isKakaoPostEnabled = false;
        if (localSettings.Values.TryGetValue(IsKakaoPostEnabledSettingKey, out var storedKakaoPostEnabled) && storedKakaoPostEnabled is bool kakaoPostEnabledValue) isKakaoPostEnabled = kakaoPostEnabledValue;

        var isTimelineRefreshEnabledOnNewPost = true;
        if (localSettings.Values.TryGetValue(IsTimelineRefreshEnabledOnNewPostSettingKey, out var storedTimelineRefreshOnNewPost) && storedTimelineRefreshOnNewPost is bool timelineRefreshOnNewPostValue) isTimelineRefreshEnabledOnNewPost = timelineRefreshOnNewPostValue;

        var isTimelineRefreshEnabledOnNewShare = false;
        if (localSettings.Values.TryGetValue(IsTimelineRefreshEnabledOnNewShareSettingKey, out var storedTimelineRefreshOnNewShare) && storedTimelineRefreshOnNewShare is bool timelineRefreshOnNewShareValue) isTimelineRefreshEnabledOnNewShare = timelineRefreshOnNewShareValue;

        var isOnlyMePostContinuationPromptEnabled = true;
        if (localSettings.Values.TryGetValue(IsOnlyMePostContinuationPromptEnabledSettingKey, out var storedOnlyMePostContinuationPrompt) && storedOnlyMePostContinuationPrompt is bool onlyMePostContinuationPromptValue) isOnlyMePostContinuationPromptEnabled = onlyMePostContinuationPromptValue;

        // Compose normalized settings and return
        var applicationSettings = new ApplicationSettings
        {
            Theme = theme,
            IsAutomaticUpdateCheckEnabled = isAutomaticUpdateCheckEnabled,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            IsKakaoPostEnabled = isKakaoPostEnabled,
            IsTimelineRefreshEnabledOnNewPost = isTimelineRefreshEnabledOnNewPost,
            IsTimelineRefreshEnabledOnNewShare = isTimelineRefreshEnabledOnNewShare,
            IsOnlyMePostContinuationPromptEnabled = isOnlyMePostContinuationPromptEnabled
        };
        NormalizeSettings(applicationSettings);
        return applicationSettings;
    }

#pragma warning disable IDE0060 // Remove unused parameter
    private static void NormalizeSettings(ApplicationSettings applicationSettings)
#pragma warning restore IDE0060 // Remove unused parameter
    {
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Settings.PropertyChanged -= OnApplicationSettingsPropertyChanged;
    }

    private void OnApplicationSettingsPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (AutoSave)
        {
            SaveSettings();
        }
    }
}