using CommunityToolkit.Maui.Alerts;

namespace History.MobileClient;

public partial class AppShell : Shell
{
    public static new bool IsLoaded { get; private set; }

	public AppShell()
	{
		InitializeComponent();
        IsLoaded = true;
	}

    private static DateTime s_lastBackPressedTime = DateTime.MinValue;
    protected override bool OnBackButtonPressed()
    {
        TimeSpan timeSinceLastBackPressed = DateTime.UtcNow - s_lastBackPressedTime;
        if (timeSinceLastBackPressed.TotalMilliseconds > 2000)
        {
            s_lastBackPressedTime = DateTime.UtcNow;
            Toast.Make("나가려면 한번 더 누르세요").Show();
        }
        else Environment.Exit(0);
        return true;
    }
}