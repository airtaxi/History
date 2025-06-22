using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text.Json;
using Android;
using Android.App;
using Android.App.Job;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Util;
using Android.Views;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using History.MobileClient.DataTypes;
using History.MobileClient.Helpers;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using Plugin.Firebase.CloudMessaging;
using Plugin.Firebase.Core.Platforms.Android;
using Intent = Android.Content.Intent;

namespace History.MobileClient.Droid;

[Activity(
    MainLauncher = true,
    ConfigurationChanges = ActivityHelper.AllConfigChanges,
    WindowSoftInputMode = SoftInput.AdjustNothing | SoftInput.StateHidden
)]
[IntentFilter([Intent.ActionSend, Intent.ActionSendMultiple],
        Categories = new[] { Intent.CategoryDefault },
        DataMimeType = "*/*")]
public class MainActivity : ApplicationActivity
{
    private const string TAG = "History";

    public static event EventHandler<string> LoginCompleted;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        global::AndroidX.Core.SplashScreen.SplashScreen.InstallSplashScreen(this);

        base.OnCreate(savedInstanceState);
        NativeMedia.Platform.Init(this, savedInstanceState);

        CrossFirebase.Initialize(this);

        ScheduleJob();
        CheckNotificationPermission();
        CreateNotificationChannelIfNeeded();
        HandleIntent(Intent);
    }

    protected override void OnNewIntent(Intent intent)
    {
        base.OnNewIntent(intent);
        HandleIntent(intent);
    }

    protected override void OnActivityResult(int requestCode, Result resultCode, Intent intent)
    {
        if (NativeMedia.Platform.CheckCanProcessResult(requestCode, resultCode, intent))
            NativeMedia.Platform.OnActivityResult(requestCode, resultCode, intent);

        PlatformActivityResultHandler.OnActivityResult(requestCode, resultCode, intent);

        base.OnActivityResult(requestCode, resultCode, intent);

        if (requestCode == Constants.GoogleAuthRequestCode)
        {
            var result = Android.Gms.Auth.Api.Auth.GoogleSignInApi.GetSignInResultFromIntent(intent);
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

#pragma warning disable CA1416 // Validate platform compatibility
        bool isNotificationPermissionGranted = CheckNotificationPermissionGranted();
        if (!isNotificationPermissionGranted)
        {
            AlertDialog.Builder dialog = new AlertDialog.Builder(this);
            AlertDialog alert = dialog.Create();
            alert.SetTitle("안내");
            alert.SetMessage("푸시 알림을 받기 위해서는 알림 권한을 활성화해주세요");
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
#pragma warning restore CA1416 // Validate platform compatibility
    }

    private void CreateNotificationChannelIfNeeded()
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            CreateNotificationChannel();
    }

    private void CreateNotificationChannel()
    {
        var channelId = $"{PackageName}.push";
        var channel = new NotificationChannel(channelId, "푸시 알림", NotificationImportance.Max);
        channel.EnableLights(true);
        channel.EnableVibration(true);
        channel.SetShowBadge(true);
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


#pragma warning disable CA1422, CA1416
    private static void HandleIntent(Intent intent)
    {
        FirebaseCloudMessagingImplementation.OnNewIntent(intent);

        var action = intent.Action;
        var type = intent.Type;

        if (!string.IsNullOrEmpty(type))
        {
            if (Intent.ActionSend.Equals(action))
            {
                HandleSingleMedia(intent);
            }
            else if (Intent.ActionSendMultiple.Equals(action))
            {
                HandleMultipleMedia(intent);
            }
        }
    }

    private static void HandleSingleMedia(Intent intent)
    {
        var sharedText = intent.GetStringExtra(Intent.ExtraText);
        if (!string.IsNullOrEmpty(sharedText))
        {
            HandleSharedText(sharedText);
            return;
        }

        var mediaUri = GetParcelableExtraSafe<Android.Net.Uri>(intent, Intent.ExtraStream);
        if (mediaUri == null) return;

        var mediaInfo = AndroidMediaPickerHelper.GetMediaFile(mediaUri);
        var mediaFiles = new List<MediaFile> { mediaInfo };

        HandleMediaFiles(mediaFiles);
    }

    private static void HandleMultipleMedia(Intent intent)
    {
        var mediaUris = GetParcelableListSafe<Android.Net.Uri>(intent, Intent.ExtraStream);
        var mediaFiles = mediaUris.Select(AndroidMediaPickerHelper.GetMediaFile).ToList();

        HandleMediaFiles(mediaFiles);
    }

    private static void HandleMediaFiles(List<MediaFile> mediaFiles)
    {
        if (MainPage.IsLoaded)
        {
            //App.Page.Dispatcher.Dispatch(async () =>
            //{
            //    var page = new EditPostPage(mediaFiles);
            //    await App.PushAsync(page);
            //});
        }
        else
        {
            var mediaData = JsonSerializer.Serialize(mediaFiles);
            Preferences.Set("MediaData", mediaData);
        }
    }

    private static void HandleSharedText(string sharedText)
    {
        if (MainPage.IsLoaded)
        {
            //App.Page.Dispatcher.Dispatch(async () =>
            //{
            //    var page = new EditPostPage(sharedText);
            //    await App.PushAsync(page);
            //});
        }
        else Preferences.Set("SharedText", sharedText);
    }

    private static T GetParcelableExtraSafe<T>(Intent intent, string key) where T : Java.Lang.Object
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu) return intent.GetParcelableExtra(key, Java.Lang.Class.FromType(typeof(T))) as T;
        else return intent.GetParcelableExtra(key) as T;
    }

    private static List<T> GetParcelableListSafe<T>(Intent intent, string key) where T : Java.Lang.Object
    {
        var list = new List<T>();
        System.Collections.IList items;
        if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu) items = intent.GetParcelableArrayListExtra(key, Java.Lang.Class.FromType(typeof(T)));
        else items = intent.GetParcelableArrayListExtra(key);

        if (items != null)
        {
            foreach (var item in items)
            {
                if (item is T typedItem)
                {
                    list.Add(typedItem);
                }
            }
        }

        return list;
    }
#pragma warning restore CA1422, CA1416
}
