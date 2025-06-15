using CommunityToolkit.Mvvm.Messaging;
using History.Commons;
using History.Commons.Api.User;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using History.MobileClient.DataTypes;
using History.MobileClient.Helpers;

namespace History.MobileClient.Pages;

public partial class SettingsPage : ContentPage
{
    private bool _isInForeground;

	public SettingsPage(UserResponseDto user)
	{
        InitializeComponent();

        VersionLabel.Text = AppInfo.Current.VersionString;
        BirthdayLabel.Text = user.Birthday?.ToString("yyyy년 MM월 dd일") ?? "설정되지 않음";
        FriendListDiscovryOptionLabel.Text = user.FriendListDiscoveryOption.ToDisplayString();

        WeakReferenceMessenger.Default.Register<LoadingStateChangedMessage>(this, OnLoadingStateChangedMessageReceived);
    }

    private static void CleanupSharedVariables()
    {
        Configuration.SetValue("AccessToken", null);
        Configuration.SetValue("RefreshToken", null);

        Shared.ApiHandler = ApiHandler.Public;
        Shared.UserId = default;
        Shared.MyRank = default;
        Shared.LastUsedPostDiscoveryOption = default;
        Shared.Friends = default;
    }

    private async void OnBackImageTapped(object sender, TappedEventArgs e) => await App.PopAsync();

    private async void OnLogoutGridTapped(object sender, TappedEventArgs e)
    {
        var result = await DisplayAlert("안내", "정말로 로그아웃을 하시겠습니까?", "네", "아니오");
        if (!result) return;

        CleanupSharedVariables();

        App.Page = new LoginPage();
    }

    private async void OnWithdrawGridTapped(object sender, TappedEventArgs e)
    {
        var result = await DisplayAlert("안내", "정말로 회원 탈퇴를 하시겠습니까?", "네", "아니오");
        if (!result) return;

        result = await DisplayAlert("경고", "회원 탈퇴는 되돌릴 수 없습니다. 정말로 회원 탈퇴를 하시겠습니까?", "네", "아니오");
        if (!result) return;

        var prompt = await DisplayPromptAsync("경고", "회원 탈퇴를 하시면 이용 약관에 따라 유예 기간 없이 모든 모든 데이터가 삭제됩니다. 이에 동의하시면 아래 \"탈퇴하겠습니다\"를 따옴표 없이 입력해주세요.", "회원 탈퇴", "취소", "탈퇴하려면 \"탈퇴하겠습니다\"를 따옴표 없이 입력");
        if (prompt != "탈퇴하겠습니다") return;

        var response = await App.ExecuteRequestAsync(new Withdraw());
        if (response.IsSuccess)
        {
            CleanupSharedVariables();

            await DisplayAlert("안내", "회원 탈퇴가 완료되었습니다. 이용해 주셔서 감사합니다.", "확인");
            App.Page = new LoginPage();
        }
    }

    private async void OnFriendListDiscovryOptionGridTapped(object sender, TappedEventArgs e)
    {
        var discoveryOptions = Enum.GetValues<DiscoveryOption>().ToList();
        discoveryOptions.Remove(DiscoveryOption.SelectedUsers);
        discoveryOptions.Remove(DiscoveryOption.UnselectedUsers);

        var rawDiscoveryOptions = discoveryOptions.Select(x => x.ToDisplayString()).ToArray();
        var rawDiscoveryOption = await App.Page.DisplayActionSheet("친구 목록 공개 범위 설정", Constants.PromptCancel, null, rawDiscoveryOptions);

        if (rawDiscoveryOption == null || rawDiscoveryOption == Constants.PromptCancel) return;

        var discoveryOption = DiscoveryOptionExtensions.FromDisplayString(rawDiscoveryOption);
        var result = await App.ExecuteRequestAsync(new UpdateFriendListDiscoveryOption(discoveryOption));
        if (result.IsSuccess) FriendListDiscovryOptionLabel.Text = rawDiscoveryOption;
    }

    private async void OnBirthdayGridTapped(object sender, TappedEventArgs e) => await DisplayAlert("안내", "현재 생일 설정은 지원하지 않습니다. 추후 업데이트를 기대해 주세요.", "확인");

    private async void OnTermsGridTapped(object sender, TappedEventArgs e)
    {
#if IOS
        await Browser.Default.OpenAsync("https://history.cenox.io/terms.html", BrowserLaunchMode.SystemPreferred);
#else
        var page = new InAppBrowserPage("서비스 이용 약관", "https://history.cenox.io/terms.html");
        await App.PushModalAsync(page);
#endif
    }

    private async void OnPrivacyPolicyGridTapped(object sender, TappedEventArgs e)
    {
#if IOS
        await Browser.Default.OpenAsync("https://history.cenox.io/privacypolicy.html", BrowserLaunchMode.SystemPreferred);
#else
        var page = new InAppBrowserPage("개인정보처리방침", "https://history.cenox.io/privacypolicy.html");
        await App.PushModalAsync(page);
#endif
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _isInForeground = true;

        var safeAreaTopHeight = LayoutHelper.GetSafeAreaTopHeight();
        if (safeAreaTopHeight != 0)
        {
            var statusBarHeight = LayoutHelper.GetStatusBarHeight();
            Padding = new Thickness(Padding.Left, -(safeAreaTopHeight - statusBarHeight), Padding.Right, Padding.Bottom);
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _isInForeground = false;
    }

    private void OnLoadingStateChangedMessageReceived(object recipient, LoadingStateChangedMessage message)
    {
        var isLoading = message.Value;
        if (!_isInForeground && message.Value) return;

        Dispatcher.Dispatch(() =>
        {
            MainActivityIndicator.IsRunning = isLoading;
            IsEnabled = !isLoading;
        });
    }

    private void OnLoaded(object sender, EventArgs e)
    {
#if IOS
        AppleSwipeGestureHelper.ApplyToPage(this);
#endif
    }

    private void OnKakaoStoryLoginGridTapped(object sender, TappedEventArgs e)
    {
        var page = new KakaoStoryLoginPage();
    }
}