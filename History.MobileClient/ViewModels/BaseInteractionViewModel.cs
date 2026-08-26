using CommunityToolkit.Mvvm.Input;
using History.Commons.Enums;

namespace History.MobileClient.ViewModels;

// Base interaction (reaction/share/repost entry) view model shared by History and Kakao Story.
// Holds the surface used by the interaction/friendship templates and the virtual tap entry point.
public partial class BaseInteractionViewModel
{
    public InteractionType Type { get; init; }
    public DateTime CreatedAt { get; init; }
    public string TargetPostId { get; init; }
    public ReactionType? ReactionType { get; init; }

    public double IconSize { get; init; } = 12;
    public IMediaViewModel ProfileMedia { get; init; }
    public string FontFamily { get; init; }
    public string Glyph { get; init; }
    public Color Color { get; init; }

    [RelayCommand]
    public virtual async Task HandleTapAsync() => throw new NotSupportedException("[BaseInteractionViewModel] HandleTapAsync must be overridden");
}
