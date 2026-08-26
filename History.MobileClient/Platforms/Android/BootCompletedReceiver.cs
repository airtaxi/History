using Android.App.Job;
using Android.App;
using Android.Content;
using Android.Util;

namespace History.MobileClient;

[BroadcastReceiver(Enabled = true, Exported = true)]
[IntentFilter([Intent.ActionBootCompleted])]
public class BootCompletedReceiver : BroadcastReceiver
{
    private const string TAG = "History";

    public override void OnReceive(Context context, Intent intent)
    {
        if (intent.Action.Equals(Intent.ActionBootCompleted))
        {
            Log.Debug(TAG, "Boot completed, rescheduling jobs.");
            var jobScheduler = (JobScheduler)context.GetSystemService(Context.JobSchedulerService);
            RescheduleTokenRefreshJob(context, jobScheduler);
            RescheduleKakaoStoryNotificationJob(context, jobScheduler);
        }
    }

    private static void RescheduleTokenRefreshJob(Context context, JobScheduler jobScheduler)
    {
        var componentName = new ComponentName(context, Java.Lang.Class.FromType(typeof(TokenRefreshService)));

        // Check if the job is already scheduled
        var allPendingJobs = jobScheduler.AllPendingJobs;
        foreach (var job in allPendingJobs)
        {
            if (job.Id == 1)
            {
                Log.Debug(TAG, "Token refresh job is already scheduled.");
                return; // Job is already scheduled. return
            }
        }

        var jobInfo = new JobInfo.Builder(1, componentName)
            .SetPeriodic(Constants.TokenRefreshIntervalMilliseconds)
            .SetPersisted(true) // Persist across device reboots
            .Build();

        jobScheduler.Schedule(jobInfo);
    }

    private static void RescheduleKakaoStoryNotificationJob(Context context, JobScheduler jobScheduler)
    {
        var componentName = new ComponentName(context, Java.Lang.Class.FromType(typeof(KakaoStoryNotificationRefreshService)));

        // Check if the job is already scheduled
        var allPendingJobs = jobScheduler.AllPendingJobs;
        foreach (var job in allPendingJobs)
        {
            if (job.Id == Constants.KakaoStoryNotificationJobId)
            {
                Log.Debug(TAG, "Kakao Story notification job is already scheduled.");
                return; // Job is already scheduled. return
            }
        }

        var jobInfo = new JobInfo.Builder(Constants.KakaoStoryNotificationJobId, componentName)
            .SetPeriodic(Constants.KakaoStoryNotificationPollIntervalMilliseconds)
            .SetPersisted(true) // Persist across device reboots
            .Build();

        jobScheduler.Schedule(jobInfo);
    }
}
