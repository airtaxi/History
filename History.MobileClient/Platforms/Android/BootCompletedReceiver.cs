using Android.App.Job;
using Android.App;
using Android.Content;
using Android.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            Log.Debug(TAG, "Boot completed, rescheduling job.");
            var jobScheduler = (JobScheduler)context.GetSystemService(Context.JobSchedulerService);
            var componentName = new ComponentName(context, Java.Lang.Class.FromType(typeof(TokenRefreshService)));

            // Check if the job is already scheduled
            var allPendingJobs = jobScheduler.AllPendingJobs;
            foreach (var job in allPendingJobs)
            {
                if (job.Id == 1)
                {
                    Log.Debug(TAG, "Job is already scheduled.");
                    return; // Job is already scheduled. return
                }
            }

            var jobInfo = new JobInfo.Builder(1, componentName)
                .SetPeriodic(Constants.TokenRefreshIntervalMilliseconds)
                .SetPersisted(true) // Persist across device reboots
                .Build();

            jobScheduler.Schedule(jobInfo);
        }
    }
}
