using CommunityToolkit.Maui;
using History.MobileClient.StaggeredLayout;
using Microsoft.Extensions.Logging;
using Mopups.Hosting;
using SpeakLink;
using System.ComponentModel.Design;
using UraniumUI;

namespace History.MobileClient;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkitMediaElement()
            .UseUraniumUI()
            .UseUraniumUIMaterial()
            .UseSpeakLink()
            .ConfigureMopups()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddMaterialSymbolsFonts();
            })
            .ConfigureMauiHandlers(c =>
            {
                c.AddHandler<CollectionView, StaggeredStructuredItemsViewHandler>();
            });

        builder.Services.AddMopupsDialogs();
#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
