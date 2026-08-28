using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using History.Commons.Enums;
using Microsoft.UI.Xaml.Media;

namespace History.WindowsClient.ViewModels;

// Base interaction (reaction/share/repost entry) view model shared by History and (future) Kakao Story.
// Holds the surface used by the interaction template and the virtual tap entry point.
public abstract partial class BaseInteractionViewModel : BaseViewModel
{
    public InteractionType Type { get; init; }
    public DateTime CreatedAt { get; init; }
    public string TargetPostId { get; init; }
    public ReactionType? ReactionType { get; init; }

    public double IconSize { get; init; } = 12;
    public ImageSource ProfileImageSource { get; init; }
    public string Glyph { get; init; }
    public Brush ColorBrush { get; init; }

    [RelayCommand]
    public virtual async Task HandleTapAsync() => throw new NotSupportedException("[BaseInteractionViewModel] HandleTapAsync must be overridden");
}