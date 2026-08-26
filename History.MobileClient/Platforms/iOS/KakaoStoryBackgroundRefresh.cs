using BackgroundTasks;
using Foundation;
using History.Commons;
using History.Commons.KakaoStory;
using History.MobileClient.KakaoStory;

namespace History.MobileClient;

/// <summary>
/// Background polling of Kakao Story mails and re-uploading of the KAuth token
/// to the server while the app is suspended, via BGAppRefreshTask (the iOS
/// mirror of the Android JobService). The system decides when the task runs, so
/// it is not real-time; The task runs about 30 seconds at most, which fits the
/// single poll cycle. The poller never opens the login modal from here
/// (IsBackgroundMode).
/// </summary>
public static class KakaoStoryBackgroundRefresh
{
    private const string TaskIdentifier = "com.airtaxi.History.Commons.KakaoStoryrefresh";

    private static readonly TimeSpan s_refreshInterval = TimeSpan.FromMinutes(Constants.KakaoStoryNotificationPollIntervalMilliseconds / 60000);

    private static bool s_registering;

    /// <summary>
    /// Registers the background task handler. Must be called before the app
    /// finishes launching; the handler is invoked on a background queue.
    /// </summary>
    public static void Register()
    {
        if (s_registering) return;
        s_registering = true;

        BGTaskScheduler.Shared.Register(TaskIdentifier, null, task =>
        {
            // Schedule the next refresh before polling so a missed or failed
            // attempt never stalls the chain.
            ScheduleNext();

            var refreshTask = (BGAppRefreshTask)task;
            var isCompleted = false;
            void CompleteTask(bool success)
            {
                if (isCompleted) return;
                isCompleted = true;
                refreshTask.SetTaskCompleted(success);
            }

            // The system may expire the task when its execution budget runs
            // out; complete it then so the app is not killed mid-poll.
            task.ExpirationHandler = () => CompleteTask(false);

            Task.Run(async () =>
            {
                try
                {
                    await KakaoStoryMailPoller.PollOnceAsync();
                    await KakaoStoryUtils.UploadTokenToServerAsync();
                    CompleteTask(true);
                }
                catch (Exception exception) { System.Diagnostics.Debug.WriteLine($"Kakao Story background poll failed: {exception.Message}"); CompleteTask(false); }
            });
        });
    }

    /// <summary>
    /// Schedules the next background refresh, replacing the pending request if
    /// one already exists.
    /// </summary>
    public static void ScheduleNext()
    {
        if ((Configuration.GetValue<bool?>("KakaoStoryNotificationEnabled") ?? true) == false) return;

        BGTaskScheduler.Shared.Cancel(TaskIdentifier);

        var request = new BGAppRefreshTaskRequest(TaskIdentifier)
        {
            EarliestBeginDate = NSDate.FromTimeIntervalSinceNow(s_refreshInterval.TotalSeconds),
        };
        if (!BGTaskScheduler.Shared.Submit(request, out var error))
            System.Diagnostics.Debug.WriteLine($"Kakao Story background refresh scheduling failed: {error?.LocalizedDescription}");
    }
}
