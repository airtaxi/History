using CommunityToolkit.Mvvm.Messaging;
using History.MobileClient.DataTypes;
using History.MobileClient.Enums;
using History.MobileClient.ViewModels;
using System.ComponentModel;

namespace History.MobileClient.Pages;

public partial class PostInteractionUsersPage : ContentPage, INotifyPropertyChanged
{
    public IEnumerable<FriendshipViewModel> Users { get; private set; }
    public string NoUsersText { get; private set; }
    public bool HasNoUsers { get; private set; }

    private bool _isInForeground;


    public PostInteractionUsersPage()
    {
        InitializeComponent();
        BindingContext = this;

        WeakReferenceMessenger.Default.Register<LoadingStateChangedMessage>(this, OnLoadingStateChangedMessageReceived);
    }

    public void SetUsers(IEnumerable<FriendshipViewModel> users, PostInteractionType type)
    {
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
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _isInForeground = true;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _isInForeground = false;
    }

    private void OnLoadingStateChangedMessageReceived(object recipient, LoadingStateChangedMessage message)
    {
        if (!_isInForeground) return;

        Dispatcher.Dispatch(() =>
        {
            var isLoading = message.Value;
            MainActivityIndicator.IsRunning = isLoading;
            IsEnabled = !isLoading;
        });
    }
}
