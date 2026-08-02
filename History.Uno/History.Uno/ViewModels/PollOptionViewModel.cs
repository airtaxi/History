using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using History.Commons.DataTypes.Contents;

namespace History.Uno.ViewModels;

public partial class PollOptionViewModel : ObservableObject
{
    private readonly PollContentViewModel _parent;

    public PollOption Option { get; }
    public int Index { get; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    public string Text => Option.Text;
    public int VoteCount => Option.VoteCount;
    public double Percentage => _parent.TotalVotes > 0 ? (double)VoteCount / _parent.TotalVotes : 0;
    public string PercentageText => $"{Percentage:P0}";

    public bool ShowResults => _parent.HasVoted || _parent.IsExpired;

    public PollOptionViewModel(PollOption option, int index, bool isSelected, PollContentViewModel parent)
    {
        Option = option;
        Index = index;
        IsSelected = isSelected;
        _parent = parent;
    }

    [RelayCommand]
    public async Task SelectAsync()
    {
        if (_parent.IsExpired) return;
        await _parent.VoteAsync(Index);
    }
}
