using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace History.WindowsClient.ViewModels;

// Single poll option row for the poll edit window. The placeholder follows the row order
// and deletion is delegated to the owning PollEditWindowViewModel.
public sealed partial class PollEditOptionItemViewModel(PollEditWindowViewModel owner) : ObservableObject
{
    [ObservableProperty]
    public partial string Text { get; set; }

    [ObservableProperty]
    public partial string PlaceholderText { get; set; }

    [RelayCommand]
    private async Task Delete() => await owner.RemoveOptionAsync(this);
}
