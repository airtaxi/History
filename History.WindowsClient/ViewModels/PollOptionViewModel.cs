using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using History.Commons.DataTypes.Contents;

namespace History.WindowsClient.ViewModels;

// Mirrors the MAUI PollOptionViewModel for a single poll option row.
public sealed partial class PollOptionViewModel(PollOption option, int index, bool isSelected, PollContentViewModel parent) : ObservableObject
{
    public PollOption Option { get; } = option;
    public int Index { get; } = index;

    [ObservableProperty]
    public partial bool IsSelected { get; set; } = isSelected;

    public string Text => Option.Text;
    public int VoteCount => Option.VoteCount;
    public double Percentage => parent.TotalVotes > 0 ? (double)VoteCount / parent.TotalVotes : 0;
    public string PercentageText => $"{Percentage:P0}";
    public bool ShowResults => parent.HasVoted || parent.IsExpired;

    [RelayCommand]
    private async Task SelectAsync()
    {
        if (parent.IsExpired) return;
        await parent.VoteAsync(Index);
    }
}