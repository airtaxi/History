using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using History.Commons;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using History.Commons.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Collections.ObjectModel;

namespace History.WindowsClient.ViewModels;

public partial class WriteMessageDialogViewModel : BaseMessageDialogViewModel
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsReceiverSelected))]
    [NotifyPropertyChangedFor(nameof(IsNotReceiverSelected))]
    [NotifyPropertyChangedFor(nameof(CanSend))]
    [NotifyPropertyChangedFor(nameof(ReceiverName))]
    [NotifyPropertyChangedFor(nameof(IsReceiverAdmin))]
    [NotifyPropertyChangedFor(nameof(IsReceiverModerator))]
    [NotifyPropertyChangedFor(nameof(ReceiverProfileImage))]
    public partial UserResponseDto SelectedReceiver { get; set; }

    public bool IsReceiverSelected => SelectedReceiver != null;
    public bool IsNotReceiverSelected => SelectedReceiver == null;

    public string ReceiverName => SelectedReceiver?.Nickname ?? SelectedReceiver?.Handle ?? SelectedReceiver?.UserId ?? string.Empty;
    public bool IsReceiverAdmin => SelectedReceiver?.Rank == Rank.Admin;
    public bool IsReceiverModerator => SelectedReceiver?.Rank == Rank.Moderator;
    public ImageSource ReceiverProfileImage => SelectedReceiver?.ProfileThumbnailMediaId != null ? new BitmapImage(new Uri(CommonUtils.GenerateMediaUri(SelectedReceiver.ProfileThumbnailMediaId))) : (SelectedReceiver?.ProfileMediaId != null ? new BitmapImage(new Uri(CommonUtils.GenerateMediaUri(SelectedReceiver.ProfileMediaId))) : null);

    [ObservableProperty]
    public partial ObservableCollection<HistoryFriendshipViewModel> SuggestedFriends { get; set; } = [];

    public override bool CanSend => IsReceiverSelected && base.CanSend;

    public WriteMessageDialogViewModel(BaseViewModel baseViewModel, UserResponseDto receiver = null) : base(baseViewModel)
    {
        SelectedReceiver = receiver;
        PopulateDefaultSuggestions();
    }

    private void PopulateDefaultSuggestions()
    {
        if (CommonShared.Friends == null)
        {
            SuggestedFriends = [];
            return;
        }

        SuggestedFriends = new(CommonShared.Friends.OrderByDescending(x => x.IsFavorite).ThenBy(x => x.Nickname).Select(x => new HistoryFriendshipViewModel(x, BaseViewModel) { FriendshipVisibility = Visibility.Collapsed, IsRootButtonHitTestVisible = false }));
    }

    public void FilterFriends(string query)
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

        SuggestedFriends = new(filtered.Select(x => new HistoryFriendshipViewModel(x, BaseViewModel) { FriendshipVisibility = Visibility.Collapsed, IsRootButtonHitTestVisible = false }));
    }

    [RelayCommand]
    public void SelectReceiver(UserResponseDto user) => SelectedReceiver = user;

    [RelayCommand]
    public void ClearReceiver()
    {
        SelectedReceiver = null;
        PopulateDefaultSuggestions();
    }

    protected override string ReceiverId => SelectedReceiver.UserId;

    protected override async Task<bool> ValidateAsync()
    {
        if (SelectedReceiver == null)
        {
            await BaseViewModel.ShowMessageDialogAsync(new(Constants.ErrorTitle, "받는 사람을 선택하세요."));
            return false;
        }

        return true;
    }
}
