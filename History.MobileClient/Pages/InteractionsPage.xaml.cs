using CommunityToolkit.Mvvm.Messaging;
using History.MobileClient.Messages;
using History.MobileClient.Helpers;
using History.MobileClient.ViewModels;
using System.ComponentModel;
using History.Commons.Enums;

namespace History.MobileClient.Pages;

public partial class InteractionsPage : ContentPage, INotifyPropertyChanged
{
    public IEnumerable<BaseFriendshipViewModel> ViewModels { get; private set; }
    public string NoUsersText { get; private set; }
    public bool HasNoUsers { get; private set; }

    private bool _isInForeground;


    public InteractionsPage(IEnumerable<BaseFriendshipViewModel> viewModels, InteractionType type, string customTitle = null)
    {
        InitializeComponent();
        BindingContext = this;

        ViewModels = viewModels;
        HasNoUsers = !viewModels.Any();
        var isKakao = viewModels.FirstOrDefault() is KakaoFriendshipViewModel;
        if (HasNoUsers)
        {
            if (customTitle != null) NoUsersText = "아직 해당 목록이 없습니다";
            else if (type == InteractionType.Reaction) NoUsersText = "느낌을 단 사용자가 없습니다";
            else if (type == InteractionType.Share) NoUsersText = "공유한 사용자가 없습니다";
            else if (type == InteractionType.Repost) NoUsersText = "리포스트한 사용자가 없습니다";
            else if (type == InteractionType.CommentLike) NoUsersText = "댓글에 좋아요를 누른 사용자가 없습니다";
        }
        OnPropertyChanged(nameof(ViewModels));
        OnPropertyChanged(nameof(NoUsersText));
        OnPropertyChanged(nameof(HasNoUsers));

        if (customTitle != null) TitleLabel.Text = customTitle;
        else if (type == InteractionType.Reaction) TitleLabel.Text = "느낌 사용자 목록";
        else if (type == InteractionType.Share) TitleLabel.Text = "공유 사용자 목록";
        else if (type == InteractionType.Repost) TitleLabel.Text = isKakao ? "UP 사용자 목록" : "리포스트 사용자 목록";
        else if (type == InteractionType.CommentLike) TitleLabel.Text = "댓글 좋아요 사용자 목록";

        WeakReferenceMessenger.Default.Register<LoadingStateChangedMessage>(this, OnLoadingStateChangedMessageReceived);
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
        if (!_isInForeground && isLoading) return;

        Application.Current.Dispatcher.Dispatch(() =>
        {
            MainActivityIndicator.IsRunning = isLoading;
            IsEnabled = !isLoading;
        });
    }

    private async void OnBackImageTapped(object sender, TappedEventArgs e) => await App.PopAsync();

    private void OnLoaded(object sender, EventArgs e)
    {
#if IOS
        AppleSwipeGestureHelper.ApplyToPage(this);
#endif
    }
}
