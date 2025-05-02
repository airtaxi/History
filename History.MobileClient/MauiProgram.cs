using CommunityToolkit.Maui;
using History.MobileClient.ThirdParty.StaggeredLayout;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Foldable;
using SpeakLink;
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
            .UseFoldable()
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
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
