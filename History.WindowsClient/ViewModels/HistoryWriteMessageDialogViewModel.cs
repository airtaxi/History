using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using History.Commons;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using History.Commons.Helpers;
using History.WindowsClient.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Collections.ObjectModel;

namespace History.WindowsClient.ViewModels;

public partial class HistoryWriteMessageDialogViewModel : BaseMessageDialogViewModel
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsReceiverSelected))]
    [NotifyPropertyChangedFor(nameof(IsNotReceiverSelected))]
    [NotifyPropertyChangedFor(nameof(CanSend))]
    [NotifyPropertyChangedFor(nameof(ReceiverName))]
    [NotifyPropertyChangedFor(nameof(IsReceiverAdmin))]
    [NotifyPropertyChangedFor(nameof(IsReceiverModerator))]
    [NotifyPropertyChangedFor(nameof(ReceiverProfileImage))]
    public partial MessageReceiver Receiver { get; set; }

    public bool IsReceiverSelected => Receiver != null;
    public bool IsNotReceiverSelected => Receiver == null;

    public string ReceiverName => Receiver?.Name ?? string.Empty;
    public bool IsReceiverAdmin => Receiver?.IsAdmin == true;
    public bool IsReceiverModerator => Receiver?.IsModerator == true;
    public ImageSource ReceiverProfileImage => Receiver?.ProfileImage;

    [ObservableProperty]
    public partial ObservableCollection<BaseFriendshipViewModel> SuggestedFriends { get; set; } = [];

    public override bool CanSend => IsReceiverSelected && base.CanSend;

    public HistoryWriteMessageDialogViewModel(BaseViewModel baseViewModel, UserResponseDto receiver = null) : base(baseViewModel)
    {
        if (receiver != null) Receiver = CreateReceiver(receiver);
        PopulateDefaultSuggestions();
    }

    protected virtual void PopulateDefaultSuggestions()
    {
        if (CommonShared.Friends == null)
        {
            SuggestedFriends = [];
            return;
        }

        SuggestedFriends = new(CommonShared.Friends.OrderByDescending(x => x.IsFavorite).ThenBy(x => x.Nickname).Select(x => (BaseFriendshipViewModel)new HistoryFriendshipViewModel(x, BaseViewModel) { FriendshipVisibility = Visibility.Collapsed, IsRootButtonHitTestVisible = false }));
    }

    public virtual void FilterFriends(string query)
    {
        if (CommonShared.Friends == null)
        {
            SuggestedFriends.Clear();
            return;
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            PopulateDefaultSuggestions();
            return;
        }

        var filtered = CommonShared.Friends.Where(x => (x.Nickname != null && (x.Nickname.Contains(query, StringComparison.OrdinalIgnoreCase) || KoreanHelper.SplitToChosung(x.Nickname).Contains(query, StringComparison.OrdinalIgnoreCase))) || (x.Handle != null && x.Handle.Contains(query, StringComparison.OrdinalIgnoreCase))).OrderByDescending(x => x.IsFavorite).ThenBy(x => x.Nickname);

        SuggestedFriends = new(filtered.Select(x => (BaseFriendshipViewModel)new HistoryFriendshipViewModel(x, BaseViewModel) { FriendshipVisibility = Visibility.Collapsed, IsRootButtonHitTestVisible = false }));
    }

    [RelayCommand]
    public void SelectReceiver(BaseFriendshipViewModel friendshipViewModel)
    {
        Receiver = friendshipViewModel switch
        {
            HistoryFriendshipViewModel historyFriendshipViewModel => CreateReceiver(historyFriendshipViewModel.User),
            KakaoFriendshipViewModel kakaoFriendshipViewModel => new(kakaoFriendshipViewModel.UserId, kakaoFriendshipViewModel.Nickname, kakaoFriendshipViewModel.ProfileThumbnailImageSource, false, false),
            _ => null,
        };
    }

    [RelayCommand]
    public void ClearReceiver()
    {
        Receiver = null;
        PopulateDefaultSuggestions();
    }

    protected override string ReceiverId => Receiver?.Id;

    protected override async Task<bool> ValidateAsync()
    {
        if (Receiver == null)
        {
            await BaseViewModel.ShowMessageDialogAsync(new(Constants.ErrorTitle, "받는 사람을 선택하세요."));
            return false;
        }

        return true;
    }

    private static MessageReceiver CreateReceiver(UserResponseDto user)
    {
        ImageSource profileImage = null;
        if (user.ProfileThumbnailMediaId != null) profileImage = new BitmapImage(new Uri(CommonUtils.GenerateMediaUri(user.ProfileThumbnailMediaId)));
        else if (user.ProfileMediaId != null) profileImage = new BitmapImage(new Uri(CommonUtils.GenerateMediaUri(user.ProfileMediaId)));

        return new(user.UserId, user.Nickname ?? user.Handle ?? user.UserId, profileImage, user.Rank == Rank.Moderator, user.Rank == Rank.Admin);
    }
}
