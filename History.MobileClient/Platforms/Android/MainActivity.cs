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
using Android.Widget;
using AndroidX.Activity.Result.Contract;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using AndroidX.Core.View;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using History.Commons.Api.Friendship;
using History.Commons.Api.Post;
using History.Commons.Api.User;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using History.MobileClient.DataTypes;
using History.MobileClient.Helpers;
using History.MobileClient.Messages;
using History.MobileClient.Pages;
using Plugin.Firebase.CloudMessaging;

namespace History.MobileClient;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ResizeableActivity = true, EnableOnBackInvokedCallback = false, WindowSoftInputMode = SoftInput.AdjustResize, LaunchMode = LaunchMode.SingleTask, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
[IntentFilter(new[] { Intent.ActionSend },
        Categories = new[] { Intent.CategoryDefault },
        DataMimeType = "*/*")]
[IntentFilter(new[] { Intent.ActionSendMultiple },
        Categories = new[] { Intent.CategoryDefault },
        DataMimeType = "*/*")]
[IntentFilter(new[] { Intent.ActionView },
        Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
        DataScheme = "https",
        DataHost = "historyweb.cc",
        AutoVerify = true)]
public class MainActivity : MauiAppCompatActivity
{
    private const string TAG = "History";
    private Android.Views.View _contentView;
    private bool _isKeyboardVisible = false;
    private int _lastKeyboardHeight = 0;

    private static MainActivity _instance;

    public static event EventHandler<string> LoginCompleted;

    protected override void OnCreate(Bundle savedInstanceState)
    {
        // A for-result share intent (e.g., the Google app) may launch a second
        // instance under SingleTask. Forward the incoming intent to the existing
        // instance, bring its task forward, and close this duplicate instead of
        // spinning up a second MAUI window.
        if (_instance != null)
        {
            _instance.HandleIntent(Intent);
            MoveExistingTaskToFront();
            Finish();
            return;
        }

        _instance = this;
        base.OnCreate(savedInstanceState);

        ScheduleJob();
        ScheduleKakaoStoryNotificationJob();
        CheckNotificationPermission();
        CreateNotificationChannelIfNeeded();
        HandleIntent(Intent);

        FirebaseCloudMessagingImplementation.NotificationBuilderProvider = notification => new NotificationCompat.Builder(this, $"{PackageName}.push")
            .SetSmallIcon(Resource.Mipmap.appicon)
            .SetContentTitle(notification.Title)
            .SetContentText(notification.Body)
            .SetPriority(NotificationCompat.PriorityDefault)
            .SetAutoCancel(true);

        NativeMedia.Platform.Init(this, savedInstanceState);

        // Register the media picker launcher here (before the activity is resumed).
        // Using ActivityResultLauncher instead of StartActivityForResult keeps the
        // result delivery working with the system chooser under SingleTask launch mode.
        var mediaPickerLauncher = RegisterForActivityResult(
            new ActivityResultContracts.StartActivityForResult(),
            new AndroidMediaPickerHelper.MediaPickActivityResultCallback());
        AndroidMediaPickerHelper.Initialize(mediaPickerLauncher);

        Window.SetSoftInputMode(SoftInput.AdjustResize | SoftInput.StateHidden);

        SetupKeyboardDetection();
    }

    protected override void OnNewIntent(Intent intent)
    {
        base.OnNewIntent(intent);
        HandleIntent(intent);
    }

    protected override void OnDestroy()
    {
        if (_instance == this) _instance = null;
        base.OnDestroy();
    }

    private void MoveExistingTaskToFront()
    {
        if (_instance == null) return;
        var activityManager = (ActivityManager)GetSystemService(ActivityService);
        activityManager.MoveTaskToFront(_instance.TaskId, MoveTaskFlags.WithHome);
    }

    private void SetupKeyboardDetection()
    {
        _contentView = FindViewById(Android.Resource.Id.Content);

        if (_contentView != null)
        {
            ViewCompat.SetOnApplyWindowInsetsListener(_contentView, new WindowInsetsListener(this));

            WindowCompat.SetDecorFitsSystemWindows(Window, false);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
            {
#pragma warning disable CA1416 // Validate platform compatibility
                var controller = Window.InsetsController;
                if (controller != null)
                {
                    controller.SystemBarsBehavior = WindowInsetsControllerCompat.BehaviorShowTransientBarsBySwipe;
                }
#pragma warning restore CA1416 // Validate platform compatibility
            }
        }
    }

    public void OnKeyboardInsetsChanged(int keyboardHeight)
    {
        bool isKeyboardVisible = keyboardHeight > 0;

        if (isKeyboardVisible != _isKeyboardVisible || keyboardHeight != _lastKeyboardHeight)
        {
            _isKeyboardVisible = isKeyboardVisible;
            _lastKeyboardHeight = keyboardHeight;

            var density = Resources.DisplayMetrics.Density;
            double keyboardHeightInDp = keyboardHeight / density;

            WeakReferenceMessenger.Default.Send(new KeyboardSizeMessage(keyboardHeightInDp));

            System.Diagnostics.Debug.WriteLine($"Keyboard: {(isKeyboardVisible ? "Shown" : "Hidden")}, Height: {keyboardHeightInDp}dp");
        }
    }

    public override void OnConfigurationChanged(Android.Content.Res.Configuration newConfig)
    {
        base.OnConfigurationChanged(newConfig);

        Task.Delay(200).ContinueWith(_ =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _isKeyboardVisible = false;
                _lastKeyboardHeight = 0;
                WeakReferenceMessenger.Default.Send(new KeyboardSizeMessage(0));
            });
        });
    }

    protected override void OnPause()
    {
        base.OnPause();
        if (_isKeyboardVisible)
        {
            _isKeyboardVisible = false;
            _lastKeyboardHeight = 0;
            WeakReferenceMessenger.Default.Send(new KeyboardSizeMessage(0));
        }
    }

    private async void UpdateNotificationContext(IDictionary<string, string> data)
    {
        if (data == null) return;
        if (!data.TryGetValue("Type", out var rawType) || !Enum.TryParse<NotificationType>(rawType, out var type)) return;
        else if (Shared.ApiHandler == null) return;

        try
        {
            if (data.TryGetValue("PostId", out var postId))
            {
                var post = await Shared.ApiHandler.ExecuteRequestAsync(new GetPost(postId));
                if (post != null) MainThread.BeginInvokeOnMainThread(() => WeakReferenceMessenger.Default.Send(new ValueChangedMessage<PostResponseDto>(post)));
            }
            else if (type == NotificationType.FriendRequest && data.TryGetValue("UserId", out var userId))
            {
                var user = await Shared.ApiHandler.ExecuteRequestAsync(new GetUser(userId));
                MainThread.BeginInvokeOnMainThread(() => WeakReferenceMessenger.Default.Send(new ValueChangedMessage<UserResponseDto>(user)));
            }

            var notifications = await Shared.ApiHandler.ExecuteRequestAsync(new GetNotifications());
            MainThread.BeginInvokeOnMainThread(() => WeakReferenceMessenger.Default.Send(new NotificationsMessage(notifications)));

            var friends = await Shared.ApiHandler.ExecuteRequestAsync(new GetFriends(Shared.UserId));
            Shared.Friends = friends;
        }
        catch { }
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

    private void ScheduleKakaoStoryNotificationJob()
    {
        try
        {
            var jobScheduler = (JobScheduler)GetSystemService(JobSchedulerService);
            var componentName = new ComponentName(this, Java.Lang.Class.FromType(typeof(KakaoStoryNotificationRefreshService)));

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

            var result = jobScheduler.Schedule(jobInfo);
            if (result == JobScheduler.ResultSuccess) Log.Debug(TAG, "Kakao Story notification job scheduled successfully.");
            else Log.Debug(TAG, "Kakao Story notification job scheduling failed.");
        }
        catch (Exception exception)
        {
            Log.Error(TAG, $"Kakao Story notification job scheduling failed: {exception.Message}\n{exception.StackTrace}");
        }
    }

#pragma warning disable CA1422, CA1416
    private void HandleIntent(Intent intent)
    {
        FirebaseCloudMessagingImplementation.OnNewIntent(intent);

        var scheme = intent.GetStringExtra(KakaoStoryNotificationPoster.SchemeExtraKey);
        if (!string.IsNullOrEmpty(scheme))
        {
            _ = App.HandleKakaoStoryNotificationAsync(scheme);
            return;
        }

        var action = intent.Action;
        var type = intent.Type;

        // App link (https://historyweb.cc/post/{postId} or /u/{userId}):
        // verified against assetlinks.json via android:autoVerify.
        if (Intent.ActionView.Equals(action))
        {
            var appLinkData = intent.DataString;
            if (!string.IsNullOrEmpty(appLinkData))
            {
                _ = App.HandleAppLinkAsync(appLinkData);
                return;
            }
        }

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

        LoadAndHandleMediaFiles([mediaUri]);
    }

    private static void HandleMultipleMedia(Intent intent)
    {
        var mediaUris = GetParcelableListSafe<Android.Net.Uri>(intent, Intent.ExtraStream);
        LoadAndHandleMediaFiles(mediaUris);
    }

    private static async void LoadAndHandleMediaFiles(List<Android.Net.Uri> mediaUris)
    {
        List<MediaFile> mediaFiles;
        try
        {
            // Copy the shared media off the UI thread, in parallel for multiple files.
            mediaFiles = await Task.Run(() => AndroidMediaPickerHelper.LoadMediaFiles(mediaUris));
        }
        catch { return; }

        HandleMediaFiles(mediaFiles);
    }

    private static void HandleMediaFiles(List<MediaFile> mediaFiles)
    {
        if (AppShell.IsLoaded)
        {
            mediaFiles = [.. mediaFiles.Where(m =>
                // Image formats
                   m.FileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                || m.FileName.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
                || m.FileName.EndsWith(".tiff", StringComparison.OrdinalIgnoreCase)
                || m.FileName.EndsWith(".heic", StringComparison.OrdinalIgnoreCase)
                || m.FileName.EndsWith(".heif", StringComparison.OrdinalIgnoreCase)
                || m.FileName.EndsWith(".webp", StringComparison.OrdinalIgnoreCase)
                || m.FileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                || m.FileName.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase)
                || m.FileName.EndsWith(".gif", StringComparison.OrdinalIgnoreCase)
                || m.FileName.EndsWith(".jxl", StringComparison.OrdinalIgnoreCase)
                || m.FileName.EndsWith(".ico", StringComparison.OrdinalIgnoreCase)
                || m.FileName.EndsWith(".avif", StringComparison.OrdinalIgnoreCase)
                // Video formats
                || m.FileName.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase)
                || m.FileName.EndsWith(".mov", StringComparison.OrdinalIgnoreCase)
                || m.FileName.EndsWith(".wmv", StringComparison.OrdinalIgnoreCase)
                || m.FileName.EndsWith(".webm", StringComparison.OrdinalIgnoreCase)
                || m.FileName.EndsWith(".avi", StringComparison.OrdinalIgnoreCase)
                || m.FileName.EndsWith(".flv", StringComparison.OrdinalIgnoreCase)
                || m.FileName.EndsWith(".mkv", StringComparison.OrdinalIgnoreCase)
                || m.FileName.EndsWith(".ogv", StringComparison.OrdinalIgnoreCase)
                || m.FileName.EndsWith(".3gp", StringComparison.OrdinalIgnoreCase)
            )];

            if (mediaFiles.Count == 0)
            {
                Toast.MakeText(Platform.AppContext, "올바르지 않은 미디어 형식을 공유하였습니다.", ToastLength.Long).Show();
                return;
            }

            App.Page.Dispatcher.Dispatch(async () =>
            {
                var page = new EditPostPage(mediaFiles);
                await App.PushAsync(page);
            });
        }
        else
        {
            var mediaData = JsonSerializer.Serialize(mediaFiles);
            Preferences.Set("MediaData", mediaData);
        }
    }

    private static void HandleSharedText(string sharedText)
    {
        if (AppShell.IsLoaded)
        {
            App.Page.Dispatcher.Dispatch(async () =>
            {
                var page = new EditPostPage(sharedText);
                await App.PushAsync(page);
            });
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
        System.Collections.IList items = null;

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

public class WindowInsetsListener : Java.Lang.Object, IOnApplyWindowInsetsListener
{
    private readonly MainActivity _activity;

    public WindowInsetsListener(MainActivity activity)
    {
        _activity = activity;
    }

    public WindowInsetsCompat OnApplyWindowInsets(Android.Views.View v, WindowInsetsCompat insets)
    {
        var imeInsets = insets.GetInsets(WindowInsetsCompat.Type.Ime());
        int keyboardHeight = imeInsets.Bottom;

        _activity.OnKeyboardInsetsChanged(keyboardHeight);

        return insets;
    }
}
