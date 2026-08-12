using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using History.MobileClient.Messages;
using static History.MobileClient.KakaoStory.KakaoStoryApiHandler.DataType;

namespace History.MobileClient.ViewModels;

// Selectable friend view model for the Kakao Story batch delete page (Plus pattern).
// Holds the friend profile surface plus the checkbox selection state.
public partial class SelectableKakaoFriendViewModel(FriendData.Profile profile) : ObservableObject
{
    public FriendData.Profile Profile { get; } = profile;

    public string Id => Profile.id;
    public string Nickname => Profile.display_name ?? "알 수 없는 사용자";
    public IMediaViewModel ProfileMedia => Profile.profile_thumbnail_url != null ? new ImageViewModel(Profile.profile_thumbnail_url) : null;

    public bool IsBlocked => Profile.blocked == true;
    public bool IsFavorite => Profile.is_favorite;

    public bool IsBlockedBadgeVisible => IsBlocked;
    public bool IsFavoriteBadgeVisible => IsFavorite;

    private bool _isSelected;

    public bool IsSelected
    {
        get => _isSelected;
        set => SetSelected(value);
    }

    public void SetSelected(bool isSelected, bool notify = true)
    {
        if (_isSelected == isSelected) return;

        _isSelected = isSelected;
        OnPropertyChanged(nameof(IsSelected));

        if (notify) WeakReferenceMessenger.Default.Send(new KakaoFriendSelectionChangedMessage());
    }

    [RelayCommand]
    public void HandleTap() => IsSelected = !IsSelected;
}
