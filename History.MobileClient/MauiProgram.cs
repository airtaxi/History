using CommunityToolkit.Maui;
using FFImageLoading.Maui;
using History.MobileClient.ThirdParty.StaggeredLayout;
using SpeakLink;
using UraniumUI;
using Microsoft.Maui.LifecycleEvents;
using Plugin.Firebase.CloudMessaging;
using Microsoft.Extensions.Logging;
using Plugin.Firebase.CloudMessaging.EventArgs;
using System.Text.Json;
using History.Commons.Enums;
using History.Commons.Api.Post;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Api.User;

using History.Commons.Api.Friendship;
using History.MobileClient.DataTypes;






#if IOS
using Plugin.Firebase.Core.Platforms.iOS;
#elif ANDROID
using Plugin.Firebase.Core.Platforms.Android;
#endif

namespace History.MobileClient;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder()
            .UseMauiApp<App>()
            .UseMauiCommunityToolkitMediaElement()
            .UseFFImageLoading()
            .UseMauiCommunityToolkit()
            .UseUraniumUI()
            .UseUraniumUIMaterial()
            .UseSpeakLink()
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
            })
            .RegisterFirebaseServices();

#if IOS
        Microsoft.Maui.Handlers.SearchBarHandler.Mapper.AppendToMapping("NoBackground", (h, v) =>
        {
            h.PlatformView.SearchBarStyle = UIKit.UISearchBarStyle.Minimal;
        });
#endif

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

    private static MauiAppBuilder RegisterFirebaseServices(this MauiAppBuilder builder)
    {
        builder.ConfigureLifecycleEvents(events => {
#if IOS
            CrossFirebaseCloudMessaging.Current.NotificationTapped += OnNotificationTapped;
            CrossFirebaseCloudMessaging.Current.NotificationReceived += OnNotificationReceived;
            events.AddiOS(iOS => iOS.WillFinishLaunching((_,__) => {
                CrossFirebase.Initialize();
                FirebaseCloudMessagingImplementation.Initialize();
                return false;
            }));
#elif ANDROID
            events.AddAndroid(android => android.OnCreate((activity, _) =>
                CrossFirebase.Initialize(activity)));
#endif
        });

        return builder;
    }

#if IOS
    private static async void OnNotificationTapped(object sender, FCMNotificationTappedEventArgs e)
    {
        var data = e.Notification.Data;

        var pushData = JsonSerializer.Serialize(data);
        if (!AppShell.IsLoaded) Preferences.Set("PushData", pushData);
        else await App.HandlePushNotificationAsync(pushData);
    }

    private static void OnNotificationReceived(object sender, FCMNotificationReceivedEventArgs e) => UpdateNotificationContext(e.Notification.Data);

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
                MainThread.BeginInvokeOnMainThread(() => WeakReferenceMessenger.Default.Send(new ValueChangedMessage<PostResponseDto>(post)));
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
#endif
}
