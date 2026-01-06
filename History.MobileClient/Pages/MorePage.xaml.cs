using History.Commons.Enums;
using History.MobileClient.Helpers;

namespace History.MobileClient.Pages;

public partial class MorePage : ContentPage
{
    public MorePage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        var safeAreaTopHeight = LayoutHelper.GetSafeAreaTopHeight();
        if (safeAreaTopHeight != 0)
        {
            var statusBarHeight = LayoutHelper.GetStatusBarHeight();
            Padding = new Thickness(Padding.Left, -(safeAreaTopHeight - statusBarHeight), Padding.Right, Padding.Bottom);
        }

        // 관리자 메뉴 표시 여부
        var isModerator = Shared.MyRank >= Rank.Moderator;
        ModerationDivider.IsVisible = isModerator;
        ModerationRecordsGrid.IsVisible = isModerator;
    }

    private async void OnPublicPostGridTapped(object sender, TappedEventArgs e)
    {
        var page = new PublicPostPage();
        await App.PushAsync(page);
    }

    private async void OnStickersGridTapped(object sender, TappedEventArgs e)
    {
        var page = new StickersPage();
        await App.PushAsync(page);
    }

    private async void OnModerationRecordsGridTapped(object sender, TappedEventArgs e)
    {
        var page = new ModerationRecordsPage();
        await App.PushAsync(page);
    }
}
