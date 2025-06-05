using CommunityToolkit.Mvvm.Messaging;
using History.MobileClient.DataTypes;
using History.MobileClient.Enums;
using History.MobileClient.Helpers;
using History.MobileClient.ViewModels;
using System.ComponentModel;

namespace History.MobileClient.Pages;

public partial class PostInteractionsPage : ContentPage, INotifyPropertyChanged
{
    public IEnumerable<FriendshipViewModel> Users { get; private set; }
    public string NoUsersText { get; private set; }
    public bool HasNoUsers { get; private set; }

    private bool _isInForeground;


    public PostInteractionsPage(IEnumerable<FriendshipViewModel> users, PostInteractionType type)
    {
        InitializeComponent();
        BindingContext = this;

        Users = users;
        HasNoUsers = !users.Any();
        if (HasNoUsers)
        {
            if (type == PostInteractionType.Reaction) NoUsersText = "느낌을 단 사용자가 없습니다";
            else if (type == PostInteractionType.Share) NoUsersText = "공유한 사용자가 없습니다";
            else if (type == PostInteractionType.Repost) NoUsersText = "리포스트한 사용자가 없습니다";
        }
        OnPropertyChanged(nameof(Users));
        OnPropertyChanged(nameof(NoUsersText));
        OnPropertyChanged(nameof(HasNoUsers));

        if (type == PostInteractionType.Reaction) TitleLabel.Text = "느낌 사용자 목록";
        else if (type == PostInteractionType.Share) TitleLabel.Text = "공유 사용자 목록";
        else if (type == PostInteractionType.Repost) TitleLabel.Text = "리포스트 사용자 목록";

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
        if (!_isInForeground && message.Value) return;

        Dispatcher.Dispatch(() =>
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
