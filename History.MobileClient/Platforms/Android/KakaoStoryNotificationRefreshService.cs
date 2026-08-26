using Android.App;
using Android.App.Job;
using Android.Util;
using History.Commons.KakaoStory;
using History.MobileClient.KakaoStory;

namespace History.MobileClient;

/// <summary>
/// Background JobService that polls Kakao Story mails and re-uploads the KAuth
/// token to the server while the app is not running (15-minute cadence,
/// JobScheduler minimum). Notification polling is owned by the server (FCM
/// push); the poller never opens the login modal from here.
/// </summary>
[Service(Name = "com.airtaxi.History.Commons.KakaoStoryNotificationRefreshService", Permission = "android.permission.BIND_JOB_SERVICE")]
public class KakaoStoryNotificationRefreshService : JobService
{
    private const string TAG = "History";

    public override bool OnStartJob(JobParameters jobParameters)
    {
        Log.Debug(TAG, "Kakao Story notification job started.");

        Task.Run(async () =>
        {
            try
            {
                await KakaoStoryMailPoller.PollOnceAsync();
                await KakaoStoryUtils.UploadTokenToServerAsync();
            }
            catch (Exception exception) { Log.Error(TAG, $"Kakao Story notification poll failed: {exception.Message}"); }
            finally { JobFinished(jobParameters, false); }
        });

        return true; // Return true if the job is still running (e.g., in a separate thread)
    }

    public override bool OnStopJob(JobParameters jobParameters)
    {
        Log.Debug(TAG, "Kakao Story notification job stopped.");
        return false; // Periodic jobs are rescheduled by the system automatically.
    }
}
