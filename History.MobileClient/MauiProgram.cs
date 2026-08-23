using CommunityToolkit.Maui;
using FFImageLoading.Maui;
using History.MobileClient.ThirdParty.StaggeredLayout;
using SuggestingBox.Maui;
using UraniumUI;
using Microsoft.Maui.LifecycleEvents;
using System.Text.Json;
using History.Commons.Enums;
using History.Commons.Api.Post;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Api.User;
using History.Commons.Api.Friendship;
using History.MobileClient.DataTypes;
using History.MobileClient.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Platform;
using History.Commons;
using Syncfusion.Maui.Toolkit.Hosting;
using Syncfusion.Maui.Core.Hosting;
using History.Commons.Api.Message;
using CommunityToolkit.Maui.Core;
using Microsoft.Extensions.DependencyInjection;

#if IOS
using Plugin.Firebase.Core.Platforms.iOS;
#elif ANDROID
using Plugin.Firebase.Core.Platforms.Android;
#endif

#if !WINDOWS
using Plugin.Firebase.CloudMessaging;
using Plugin.Firebase.CloudMessaging.EventArgs;
#endif

namespace History.MobileClient;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1JAaF5cX2pCd1p/TH5YfUNzdUVEY1ZUTXxaS1ZhSXxVdkJhXH9bdXRVTmBeV0B9XEY=");

        ApiHandler.ApplicationVersion = AppInfo.Current.VersionString;
        ApiHandler.Platform = DeviceInfo.Platform.ToString();

        var builder = MauiApp.CreateBuilder()
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseMauiCommunityToolkitMediaElement(false, (options) =>
            {
                options.SetDefaultAndroidViewType(AndroidViewType.TextureView);
                options.SetIsAndroidForegroundServiceEnabled(false);
            })
            .UseFFImageLoading()
            .UseUraniumUI()
            .UseUraniumUIMaterial()
            .UseSuggestingBox()
            .ConfigureSyncfusionToolkit()
            .ConfigureSyncfusionCore()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddMaterialSymbolsFonts();
                fonts.AddFontAwesomeIconFonts();
            })
            .ConfigureMauiHandlers(c =>
            {
                c.AddHandler<CollectionView, StaggeredStructuredItemsViewHandler>();
#if IOS
                c.AddHandler(typeof(Shell), typeof(CustomShellRenderer));
#endif
#if ANDROID
                c.AddHandler(typeof(Shell), typeof(AndroidShellRenderer));
#endif
            })
            .RegisterFirebaseServices();

        builder.Services.AddMauiBlazorWebView();

#if IOS
        Microsoft.Maui.Handlers.SearchBarHandler.Mapper.AppendToMapping("NoBackground", (h, v) =>
        {
            h.PlatformView.SearchBarStyle = UIKit.UISearchBarStyle.Minimal;
        });
        Microsoft.Maui.Handlers.PickerHandler.Mapper.AppendToMapping("NoOutline", (h, v) =>
        {
            h.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;
            h.PlatformView.BackgroundColor = UIKit.UIColor.Clear;
            h.PlatformView.Layer.BorderWidth = 0;
        });
#elif ANDROID
        Microsoft.Maui.Handlers.PickerHandler.Mapper.AppendToMapping("NoUnderline", (h, v) =>
        {
            h.PlatformView.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Colors.Transparent.ToPlatform());
        });
#endif

#if DEBUG
        builder.Logging.AddDebug();
#endif

#if ANDROID
        CrossFirebaseCloudMessaging.Current.NotificationTapped += OnNotificationTapped;
        CrossFirebaseCloudMessaging.Current.NotificationReceived += OnNotificationReceived;

        // CoreCLR GC: prefer background GC to minimize UI thread pauses during scroll.
        System.Runtime.GCSettings.LatencyMode = System.Runtime.GCLatencyMode.SustainedLowLatency;
#endif

        return builder.Build();
    }

    private static MauiAppBuilder RegisterFirebaseServices(this MauiAppBuilder builder)
    {
        builder.ConfigureLifecycleEvents(events => {
#if IOS
            events.AddiOS(iOS => iOS.WillFinishLaunching((_,__) => {
                CrossFirebaseCloudMessaging.Current.NotificationTapped += OnNotificationTapped;
                CrossFirebaseCloudMessaging.Current.NotificationReceived += OnNotificationReceived;

                CrossFirebase.Initialize();
                FirebaseCloudMessagingImplementation.Initialize();
                return false;
            }));
#elif ANDROID
            events.AddAndroid(android => android.OnCreate((activity, _) => CrossFirebase.Initialize(activity, () => Platform.CurrentActivity)));
#endif
        });

        return builder;
    }

#if !WINDOWS
    private static void OnNotificationTapped(object sender, FCMNotificationTappedEventArgs e)
    {
        var data = e.Notification.Data;

        Application.Current.Dispatcher.Dispatch(async () =>
        {
            var pushData = JsonSerializer.Serialize(data);
            if (!AppShell.IsLoaded) Preferences.Set("PushData", pushData);
            else await App.HandlePushNotificationAsync(pushData);
        });
    }

    private static void OnNotificationReceived(object sender, FCMNotificationReceivedEventArgs e) => UpdateNotificationContext(e.Notification.Data);
#endif

    private static async void UpdateNotificationContext(IDictionary<string, string> data)
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
}
