using CommunityToolkit.Maui;
using FFImageLoading.Maui;
using History.MobileClient.ThirdParty.StaggeredLayout;
using SpeakLink;
using UraniumUI;
using Microsoft.Maui.LifecycleEvents;
using Plugin.Firebase.CloudMessaging;



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
}
