using Android.App;
using Android.App.Job;
using Android.Util;
using History.Commons;
using History.Commons.Api.User;

namespace History.MobileClient;

[Service(Name = "com.airtaxi.history.TokenRefreshService", Permission = "android.permission.BIND_JOB_SERVICE")]
public class TokenRefreshService : JobService
{
    private const string TAG = "History";

    public override bool OnStartJob(JobParameters jobParameters)
    {
        Log.Debug(TAG, "Token refresh job started.");
        
        Task.Run(async () => {
            // Simulate token refresh
            try
            {
                var accessToken = Configuration.GetValue<string>("AccessToken");
                var refreshToken = Configuration.GetValue<string>("RefreshToken");
                if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken)) return;

                var result = await ApiHandler.Public.ExecuteRequestAsync(new RefreshToken(refreshToken));
                accessToken = result.AccessToken;
                refreshToken = result.RefreshToken;

                Shared.ApiHandler = new ApiHandler(accessToken, refreshToken);

                Configuration.SetValue("AccessToken", accessToken);
                Configuration.SetValue("RefreshToken", refreshToken);

                Log.Debug(TAG, "Token refreshed.");
            }
            catch (Exception exception)
            {
                Log.Error(TAG, $"Token refresh failed: {exception.Message}");
                return;
            }
            finally
            {
                // Reschedule the job to run again
                JobFinished(jobParameters, true);
            }
        });

        return true; // Return true if the job is still running (e.g., in a separate thread)
    }

    public override bool OnStopJob(JobParameters jobParameters)
    {
        Log.Debug(TAG, "Token refresh job stopped.");
        return true; // Return true to reschedule the job if interrupted
    }
}
