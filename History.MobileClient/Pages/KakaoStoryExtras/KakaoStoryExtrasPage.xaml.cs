using History.MobileClient.Helpers;

namespace History.MobileClient.Pages;

public partial class KakaoStoryExtrasPage : ContentPage
{
    public KakaoStoryExtrasPage()
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
    }

    private async void OnBackImageTapped(object sender, TappedEventArgs e) => await App.PopModalAsync();

    private async void OnBatchDeleteFriendsGridTapped(object sender, TappedEventArgs e)
    {
        var page = new BatchDeleteFriendsPage();
        await App.PushAsync(page);
    }

    private async void OnBatchDeletePostsGridTapped(object sender, TappedEventArgs e)
    {
        var page = new BatchManagePostsPage(isDeleteMode: true);
        await App.PushAsync(page);
    }

    private async void OnBatchChangePermissionGridTapped(object sender, TappedEventArgs e)
    {
        var page = new BatchManagePostsPage(isDeleteMode: false);
        await App.PushAsync(page);
    }

    private void OnLoaded(object sender, EventArgs e)
    {
#if IOS
        AppleSwipeGestureHelper.ApplyToPage(this);
#endif
    }
}
