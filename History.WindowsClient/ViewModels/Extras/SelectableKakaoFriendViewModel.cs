using CommunityToolkit.Mvvm.ComponentModel;
using static History.Commons.KakaoStory.KakaoStoryApiHandler.DataType;

namespace History.WindowsClient.ViewModels.Extras;

// A Kakao Story friend row in the batch delete list: adds selection toggling and the
// restricted-user identifier on top of the shared friendship row surface.
public sealed partial class SelectableKakaoFriendViewModel(FriendData.Profile profile, BatchDeleteFriendsPageViewModel pageViewModel) : KakaoFriendshipViewModel(profile, pageViewModel)
{
    // Raised whenever the selection state changes so the owner can refresh its summary.
    public event EventHandler SelectionChanged;

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    // Kakao-suspended users (distinct from users blocked by me) are identified by their id,
    // so the list can be filtered by which entries are restricted.
    public bool IsRestricted { get; } = profile.blocked == true;

    // Tapping a row toggles its selection instead of opening the profile.
    public override Task HandleTapAsync()
    {
        SetSelected(!IsSelected);
        return Task.CompletedTask;
    }

    public void SetSelected(bool isSelected)
    {
        if (IsSelected == isSelected) return;

        IsSelected = isSelected;
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }
}
