using CommunityToolkit.Mvvm.Messaging;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using History.Commons.Enums;
using History.MobileClient.Messages;

namespace History.MobileClient.ViewModels;

public partial class SelectablePostViewModel : HistoryPostViewModel
{
    public SelectablePostViewModel(PostResponseDto post) : base(post, PostType.Timeline) => IsSelectable = true;

    public override async Task HandleTapAsync()
    {
        IsSelected = !IsSelected;
        WeakReferenceMessenger.Default.Send(new PostSelectionChangedMessage());
    }

    public void SetSelected(bool isSelected)
    {
        IsSelected = isSelected;
        WeakReferenceMessenger.Default.Send(new PostSelectionChangedMessage());
    }

    public void ApplyDiscoveryOption(DiscoveryOption discoveryOption) => DiscoveryOptionGlyph = Utils.GetDiscoveryOptionGlyph(discoveryOption);
}
