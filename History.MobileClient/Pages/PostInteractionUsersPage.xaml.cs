using History.MobileClient.Enums;
using History.MobileClient.ViewModels;
using System.ComponentModel;

namespace History.MobileClient.Pages;

public partial class PostInteractionUsersPage : ContentPage, INotifyPropertyChanged
{
    public IEnumerable<FriendshipViewModel> Users { get; private set; }
    public string NoUsersText { get; private set; }
    public bool HasNoUsers { get; private set; }


    public PostInteractionUsersPage()
    {
        InitializeComponent();
        BindingContext = this;
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
}
