using Android.Content;
using AndroidX.Core.Content;
using History.Commons;

namespace History.MobileClient;

/// <summary>
/// Starts and stops the Kakao Story realtime notification foreground service.
/// The service is only started from the settings page (app in the foreground),
/// so the Android 12+ background start restriction never applies. The service
/// is also restarted from MainActivity.OnCreate when the setting is enabled,
/// which re-arms it after the Android 15 dataSync 6-hour timeout.
/// </summary>
public static class KakaoStoryRealtimeNotificationManager
{
    public const string EnabledKey = "KakaoStoryRealtimeNotificationEnabled";

    public static bool IsEnabled => Configuration.GetValue<bool?>(EnabledKey) ?? false;

    public static void Start()
    {
        if (KakaoStoryRealtimeNotificationService.IsRunning) return;

        var context = Platform.AppContext;
        var intent = new Intent(context, typeof(KakaoStoryRealtimeNotificationService));
        ContextCompat.StartForegroundService(context, intent);
    }

    /// <summary>
    /// Starts the service when the setting is enabled. Failures are logged
    /// instead of thrown so login flows never break on a service start error.
    /// </summary>
    public static void StartIfEnabled()
    {
        if (!IsEnabled) return;

        try { Start(); }
        catch (Exception exception) { Android.Util.Log.Error("History", $"Kakao Story realtime notification service start failed: {exception.Message}"); }
    }

    public static void Stop()
    {
        var context = Platform.AppContext;
        context.StopService(new Intent(context, typeof(KakaoStoryRealtimeNotificationService)));
    }
}
