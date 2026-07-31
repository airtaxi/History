#if __ANDROID__
using Android.App;
using Android.App.Job;
using Android.Content;
using Android.Util;

namespace History.Uno.Droid;

[BroadcastReceiver(Enabled = true, Exported = true)]
[IntentFilter([Intent.ActionBootCompleted])]
public class BootCompletedReceiver : BroadcastReceiver
{
    private const string TAG = "History";

    public override void OnReceive(Context context, Intent intent)
    {
        if (intent.Action.Equals(Intent.ActionBootCompleted))
        {
            Log.Debug(TAG, "Boot completed, rescheduling job.");
            var jobScheduler = (JobScheduler)context.GetSystemService(Context.JobSchedulerService);
            var componentName = new ComponentName(context, Java.Lang.Class.FromType(typeof(TokenRefreshService)));

            var allPendingJobs = jobScheduler.AllPendingJobs;
            foreach (var job in allPendingJobs)
            {
                if (job.Id == 1)
                {
                    Log.Debug(TAG, "Job is already scheduled.");
                    return;
                }
            }

            var jobInfo = new JobInfo.Builder(1, componentName)
                .SetPeriodic(Constants.TokenRefreshIntervalMilliseconds)
                .SetPersisted(true)
                .Build();

            jobScheduler.Schedule(jobInfo);
        }
    }
}
#endif