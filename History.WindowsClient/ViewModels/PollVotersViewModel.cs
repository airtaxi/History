using CommunityToolkit.Mvvm.ComponentModel;
using History.Commons.Api.Post;
using System.Collections.ObjectModel;

namespace History.WindowsClient.ViewModels;

// Voters dialog surface: loads the voters of a single poll option through GetPollVoters
// (mirrors the MAUI PollVotersPage flow).
public sealed partial class PollVotersViewModel(BaseViewModel baseViewModel, string postId, string pollId, int optionIndex, string optionText) : ObservableObject
{
    [ObservableProperty]
    public partial ObservableCollection<PollVoterViewModel> Voters { get; set; } = [];

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    public string Title => $"'{optionText}' 투표자";

    public async Task InitializeAsync()
    {
        IsLoading = true;
        try
        {
            var result = await baseViewModel.ExecuteRequestAsync(new GetPollVoters(postId, pollId, optionIndex));
            Voters = result.IsSuccess ? new ObservableCollection<PollVoterViewModel>(result.Value.Select(voter => new PollVoterViewModel(voter))) : [];
            IsEmpty = Voters.Count == 0;
        }
        finally { IsLoading = false; }
    }
}