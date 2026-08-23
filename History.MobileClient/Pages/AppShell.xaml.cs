using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.Messaging;
using History.Commons;
using History.MobileClient.Messages;
using System.Diagnostics;

namespace History.MobileClient;

public partial class AppShell : Shell
{
    public static new bool IsLoaded { get; set; }

	public AppShell()
	{
		InitializeComponent();
        IsLoaded = true;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        var isKakaoStoryFeaturesEnabled = Configuration.GetValue<bool?>("KakaoStoryFeaturesEnabled") ?? false;
        var isGuideDismissed = Configuration.GetValue<bool?>("KakaoStoryGuideDismissed") ?? false;
        if (!isKakaoStoryFeaturesEnabled && !isGuideDismissed) _ = ShowKakaoStoryGuideAsync();
    }

    private async Task ShowKakaoStoryGuideAsync()
    {
        var doNotShowAgain = await DisplayAlertAsync("안내", "카카오스토리 기능은 프로필 → 설정에서 앱 버전을 6번 탭하면 사용할 수 있습니다.\n컴플라이언스 이슈로 비밀 기능으로 전환되는 관계로, 이 안내는 9월 중순까지만 표시될 예정이고 이후에는 이 메시지가 보여지지 않습니다.", Constants.PromptOk, "다시 보지 않기");
        if (doNotShowAgain) Configuration.SetValue("KakaoStoryGuideDismissed", true);
    }

    private static DateTime s_lastBackPressedTime = DateTime.MinValue;
    protected override bool OnBackButtonPressed()
    {
        if (Navigation.NavigationStack.Count > 1) return base.OnBackButtonPressed();

        TimeSpan timeSinceLastBackPressed = DateTime.UtcNow - s_lastBackPressedTime;
        if (timeSinceLastBackPressed.TotalMilliseconds > 2000)
        {
            s_lastBackPressedTime = DateTime.UtcNow;
            Toast.Make("나가려면 한번 더 누르세요").Show();
        }
        else Environment.Exit(0);
        return true;
    }

    protected override void OnNavigating(ShellNavigatingEventArgs args)
    {
        base.OnNavigating(args);

        // On iOS, selecting does trigger OnNavigating, but Android does not. See AndroidShellRenderer.cs for the workaround.
        if (args.Source == ShellNavigationSource.ShellSectionChanged && args.Current?.Location == args.Target?.Location) WeakReferenceMessenger.Default.Send(new TabReselectedMessage());
    }
}