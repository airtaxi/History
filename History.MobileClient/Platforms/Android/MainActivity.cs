using Android;
using Android.App;
using Android.App.Job;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Util;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using History.Commons.Api.Post;
using History.Commons.DataTypes.ResponseDtos;
using Plugin.Firebase.CloudMessaging;
using System.Runtime.Versioning;
using System.Text.Json;

namespace History.MobileClient;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ResizeableActivity = true, LaunchMode = LaunchMode.SingleTask, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    private const string TAG = "History";

    public static event EventHandler<string> LoginCompleted;

    protected override void OnCreate(Bundle savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        ScheduleJob();
        CheckNotificationPermission();
        CreateNotificationChannelIfNeeded();
        HandleIntent(Intent);

        FirebaseCloudMessagingImplementation.ShowLocalNotificationAction = notification => {
            var notificationId = Guid.NewGuid().GetHashCode();

            var intent = PackageManager.GetLaunchIntentForPackage(PackageName);
            intent.SetFlags(ActivityFlags.SingleTop);
            foreach (var entry in notification.Data) intent.PutExtra(entry.Key, entry.Value);

            var pendingIntent = PendingIntent.GetActivity(this, notificationId, intent, PendingIntentFlags.OneShot | PendingIntentFlags.Immutable);

            var builder = new NotificationCompat.Builder(this, $"{PackageName}.push")
            .SetSmallIcon(Resource.Mipmap.appicon)
            .SetContentTitle(notification.Title)
            .SetContentText(notification.Body)
            .SetContentIntent(pendingIntent)
            .SetPriority(NotificationCompat.PriorityDefault)
            .SetAutoCancel(true);

            if (notification.ImageUrl != null)
            {
                var url = new Java.Net.URL(notification.ImageUrl);
                var image = BitmapFactory.DecodeStream(url.OpenConnection().InputStream);

                builder = builder
                .SetLargeIcon(image)
                .SetStyle(new NotificationCompat.BigPictureStyle()
                    .BigPicture(image)
                    .SetSummaryText(notification.Body));
            }

            var notificationManager = (NotificationManager)GetSystemService(NotificationService);
            notificationManager.Notify(notificationId, builder.Build());

            UpdatePostIfExists(notification.Data);
        };

        NativeMedia.Platform.Init(this, savedInstanceState);
    }

    private async void UpdatePostIfExists(IDictionary<string, string> data)
    {
        if (data == null || !data.TryGetValue("PostId", out var postId)) return;
        else if (Shared.ApiHandler == null) return;

        try
        {
            var post = await Shared.ApiHandler.ExecuteRequestAsync(new GetPost(postId));
            MainThread.BeginInvokeOnMainThread(() => WeakReferenceMessenger.Default.Send(new ValueChangedMessage<PostResponseDto>(post)));
        }
        catch { }
    }

    protected override void OnNewIntent(Intent intent)
    {
        base.OnNewIntent(intent);
        HandleIntent(intent);
    }

    protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
    {
        if (NativeMedia.Platform.CheckCanProcessResult(requestCode, resultCode, data))
            NativeMedia.Platform.OnActivityResult(requestCode, resultCode, data);

        base.OnActivityResult(requestCode, resultCode, data);

        if (requestCode == Constants.GoogleAuthRequestCode)
        {
            var result = Android.Gms.Auth.Api.Auth.GoogleSignInApi.GetSignInResultFromIntent(data);
            if (result.IsSuccess)
            {
                var token = result.SignInAccount?.IdToken;
                LoginCompleted?.Invoke(this, token);
            }
            else
            {
                LoginCompleted?.Invoke(this, null);
            }
        }
    }

    private void CheckNotificationPermission()
    {
        if ((int)Build.VERSION.SdkInt < 33) return;

        bool isNotificationPermissionGranted = CheckNotificationPermissionGranted();
        if (!isNotificationPermissionGranted)
        {
            AlertDialog.Builder dialog = new AlertDialog.Builder(this);
            AlertDialog alert = dialog.Create();
            alert.SetTitle("안내");
            alert.SetMessage("푸쉬 알림을 받기 위해서는 알림 권한을 활성화해주세요");
            alert.SetButton("확인", (_, _) =>
            {
                var denied = ActivityCompat.ShouldShowRequestPermissionRationale(this, Manifest.Permission.PostNotifications);
                if (denied)
                {
                    Intent intent = new Intent("android.settings.APPLICATION_DETAILS_SETTINGS");
                    var uri = global::Android.Net.Uri.FromParts("package", PackageName, null);
                    intent.SetData(uri);
                    StartActivity(intent);
                }
                else ActivityCompat.RequestPermissions(this, new[] { Manifest.Permission.PostNotifications }, 3939);
            });
            alert.Show();
        }
    }

    private void CreateNotificationChannelIfNeeded()
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            CreateNotificationChannel();
    }

    private void CreateNotificationChannel()
    {
        var channelId = $"{PackageName}.push";
        var channel = new NotificationChannel(channelId, "푸시 알림", NotificationImportance.Default);
        var notificationManager = (NotificationManager)GetSystemService(NotificationService);
        notificationManager.CreateNotificationChannel(channel);
        FirebaseCloudMessagingImplementation.ChannelId = channelId;
    }

    private void ScheduleJob()
    {
        try
        {
            var jobScheduler = (JobScheduler)GetSystemService(JobSchedulerService);
            var componentName = new ComponentName(this, Java.Lang.Class.FromType(typeof(TokenRefreshService)));

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

            var result = jobScheduler.Schedule(jobInfo);
            if (result == JobScheduler.ResultSuccess)
            {
                Log.Debug(TAG, "Job scheduled successfully.");
            }
            else
            {
                Log.Debug(TAG, "Job scheduling failed.");
            }
        }
        catch (Exception exception)
        {
            Log.Error(TAG, $"Job scheduling failed: {exception.Message}\n{exception.StackTrace}");
        }
    }

    [SupportedOSPlatform("android33.0")]
    private static bool CheckNotificationPermissionGranted() => ContextCompat.CheckSelfPermission(Platform.AppContext, Manifest.Permission.PostNotifications) == Permission.Granted;

    private static async void HandleIntent(Intent intent)
    {
        FirebaseCloudMessagingImplementation.OnNewIntent(intent);

        if (intent?.Extras != null)
        {
            var data = new Dictionary<string, string>();

            foreach (var key in intent.Extras.KeySet()) data.Add(key, intent.Extras.GetString(key));

            if (data.Count > 0)
            {
                var pushData = JsonSerializer.Serialize(data);
                if (!AppShell.IsLoaded) Preferences.Set("PushData", pushData);
                else await App.HandlePushNotificationAsync(pushData);
            }
        }
    }
}