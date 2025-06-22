namespace History.MobileClient.MauiControls;

public partial class App : Application
{
    private static App s_instance;
    public App()
    {
        s_instance = this;
        InitializeComponent();
    }

    public static void SetAppTheme(bool isDarkMode)
    {
        if (isDarkMode) s_instance.UserAppTheme = AppTheme.Dark;
        else s_instance.UserAppTheme = AppTheme.Light;
    }
}
