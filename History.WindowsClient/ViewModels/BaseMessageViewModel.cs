using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Media;

namespace History.WindowsClient.ViewModels;

public abstract partial class BaseMessageViewModel(BaseViewModel baseViewModel) : ObservableObject
{
    protected BaseViewModel BaseViewModel { get; } = baseViewModel;

    [ObservableProperty]
    public partial string Id { get; protected set; }

    [ObservableProperty]
    public partial string SenderName { get; protected set; }

    [ObservableProperty]
    public partial bool IsSenderAdmin { get; protected set; }

    [ObservableProperty]
    public partial bool IsSenderModerator { get; protected set; }

    [ObservableProperty]
    public partial ImageSource ProfileImageSource { get; protected set; }

    [ObservableProperty]
    public partial string ReceiverName { get; protected set; }

    [ObservableProperty]
    public partial bool IsReceiverAdmin { get; protected set; }

    [ObservableProperty]
    public partial bool IsReceiverModerator { get; protected set; }

    [ObservableProperty]
    public partial ImageSource ReceiverProfileImageSource { get; protected set; }

    [ObservableProperty]
    public partial string MainText { get; protected set; }

    [ObservableProperty]
    public partial string TimestampText { get; protected set; }

    [ObservableProperty]
    public partial string DetailedTimestampText { get; protected set; }

    [ObservableProperty]
    public partial bool HasImage { get; protected set; }

    [ObservableProperty]
    public partial ImageSource ImageSource { get; protected set; }

    [ObservableProperty]
    public partial Brush MainTextForeground { get; protected set; }

    [ObservableProperty]
    public partial bool IsUnread { get; protected set; }

    [ObservableProperty]
    public partial bool IsReplyButtonVisible { get; protected set; }

    [ObservableProperty]
    public partial string ReplyButtonText { get; protected set; }

    [RelayCommand]
    public virtual async Task HandleTapAsync() => await Task.CompletedTask;

    [RelayCommand]
    public virtual async Task ReplyAsync() => await Task.CompletedTask;
}
