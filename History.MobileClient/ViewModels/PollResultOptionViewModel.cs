using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using History.MobileClient.Pages;

namespace History.MobileClient.ViewModels;

public partial class PollResultOptionViewModel : ObservableObject
{
    private readonly int _optionIndex;
    private readonly string _postId;
    private readonly string _pollId;

    public string Text { get; }
    public int VoteCount { get; }
    public int TotalVotes { get; }
    public double Percentage => TotalVotes > 0 ? (double)VoteCount / TotalVotes : 0;
    public string PercentageText => $"{Percentage:P0}";
    public string VoteCountText => $"{VoteCount}표";
    public bool HasVoters => VoteCount > 0;

    public PollResultOptionViewModel(string text, int voteCount, int totalVotes, int optionIndex, string postId, string pollId)
    {
        Text = text;
        VoteCount = voteCount;
        TotalVotes = totalVotes;
        _optionIndex = optionIndex;
        _postId = postId;
        _pollId = pollId;
    }

    [RelayCommand]
    private async Task ShowVotersAsync()
    {
        await App.PushAsync(new PollVotersPage(_postId, _pollId, _optionIndex, Text));
    }
}
