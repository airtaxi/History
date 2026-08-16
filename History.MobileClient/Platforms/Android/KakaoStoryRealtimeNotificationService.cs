using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Util;
using AndroidX.Core.App;
using History.MobileClient.KakaoStory;

namespace History.MobileClient;

/// <summary>
/// Foreground service that polls the Kakao Story notification and mail lists
/// every 10 seconds while the app is in the background. The ongoing
/// notification keeps the process alive so the poll cadence is effectively
/// real-time; the 15-minute JobService remains the fallback when this service
/// is not running (e.g. after the Android 15 dataSync 6-hour timeout, until
/// the app is opened again and MainActivity restarts it). The poll cycle is
/// the shared <see cref="KakaoStoryNotificationPoller.PollOnceAsync"/>, so the
/// Kakao Story notification setting and the login-modal suppression
/// (IsBackgroundMode) behave exactly like the other pollers.
/// </summary>
[Service(Name = "com.airtaxi.history.KakaoStoryRealtimeNotificationService", ForegroundServiceType = ForegroundService.TypeDataSync)]
public class KakaoStoryRealtimeNotificationService : Service
{
    private const string TAG = "History";
    private const int NotificationId = 9003;

    private CancellationTokenSource _pollingCts;
    private Task _pollingTask;

    public static bool IsRunning { get; private set; }

    public override void OnCreate()
    {
        base.OnCreate();
        IsRunning = true;
        CreateNotificationChannelIfNeeded();
    }

    public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
    {
        // The foreground notification must be posted within a few seconds of
        // the service start, or the system kills the process.
        StartForeground(NotificationId, BuildOngoingNotification(), ForegroundService.TypeDataSync);

        if (_pollingTask == null)
        {
            _pollingCts = new CancellationTokenSource();
            _pollingTask = RunPollingLoopAsync(_pollingCts.Token);
        }

        return StartCommandResult.Sticky;
    }

    public override void OnDestroy()
    {
        IsRunning = false;
        _pollingCts?.Cancel();
        _pollingCts = null;
        _pollingTask = null;
        base.OnDestroy();
    }

    public override IBinder OnBind(Intent intent) => null;

    /// <summary>
    /// Called when the system stops the service because the dataSync foreground
    /// service type timed out (Android 15+). The service is stopped cleanly;
    /// MainActivity restarts it the next time the app is opened.
    /// </summary>
    public override void OnTimeout(int startId, ForegroundService fgsType)
    {
        Log.Debug(TAG, "Kakao Story realtime notification service timed out.");
        StopSelf();
    }

    private async Task RunPollingLoopAsync(CancellationToken cancellationToken)
    {
        // A per-loop timer keeps a restarting loop from sharing a timer with a
        // still-finishing previous loop (PeriodicTimer only allows one waiter).
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                try { await KakaoStoryNotificationPoller.PollOnceAsync(); }
                catch (Exception exception) { Log.Error(TAG, $"Kakao Story realtime poll cycle failed: {exception.Message}"); }
            }
        }
        catch (System.OperationCanceledException) { }
    }

    private void CreateNotificationChannelIfNeeded()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O) return;

        var channelId = $"{PackageName}.realtime";
        var channel = new NotificationChannel(channelId, "실시간 알림", NotificationImportance.Low);
        channel.Description = "카카오스토리 실시간 알림을 위해 항상 표시되는 알림입니다.";
        channel.SetShowBadge(false);
        channel.EnableLights(false);
        channel.EnableVibration(false);
        channel.SetSound(null, null);
        var notificationManager = (NotificationManager)GetSystemService(NotificationService);
        notificationManager.CreateNotificationChannel(channel);
    }

    private Notification BuildOngoingNotification()
    {
        var intent = new Intent(this, typeof(MainActivity));
        intent.SetFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);
        var pendingIntent = PendingIntent.GetActivity(this, NotificationId, intent, PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);

        var builder = new NotificationCompat.Builder(this, $"{PackageName}.realtime")
            .SetSmallIcon(Resource.Mipmap.appicon)
            .SetContentTitle("카카오스토리 실시간 알림")
            .SetContentText("백그라운드에서 카카오스토리 알림을 확인하고 있습니다.")
            .SetPriority(NotificationCompat.PriorityLow)
            .SetOngoing(true)
            .SetSilent(true)
            .SetContentIntent(pendingIntent);

        return builder.Build();
    }
}
