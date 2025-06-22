using SpeakLink;

namespace History.MobileClient;

public static class AppBuilderExtensions
{
    public static MauiAppBuilder UseMauiControls(this MauiAppBuilder builder) => builder
        .ConfigureFonts(fonts =>
        {
            fonts.AddFont("Assets/Fonts/OpenSansRegular.ttf", "OpenSansRegular");
            fonts.AddFont("Assets/Fonts/OpenSansSemibold.ttf", "OpenSansSemibold");
        })
        .UseSpeakLink();
}
