using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using History.Commons.Api.Post;
using History.Commons.DataTypes.Contents;
using History.Commons.DataTypes.ResponseDtos;

namespace History.WindowsClient.ViewModels;

// Mirrors the MAUI PollContentViewModel for the poll card surface. The MAUI version receives
// data through the constructor; here the control owns a single instance and pushes data in
// through the Update method instead.
public sealed partial class PollContentViewModel : ObservableObject
{
    private string _postId;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Question))]
    [NotifyPropertyChangedFor(nameof(Options))]
    [NotifyPropertyChangedFor(nameof(TotalVotes))]
    [NotifyPropertyChangedFor(nameof(TotalVotesText))]
    [NotifyPropertyChangedFor(nameof(IsExpired))]
    [NotifyPropertyChangedFor(nameof(ExpiresAtText))]
    [NotifyPropertyChangedFor(nameof(HasVoted))]
    [NotifyPropertyChangedFor(nameof(CanVote))]
    [NotifyPropertyChangedFor(nameof(ShowResultsButton))]
    public partial PollContent PollContent { get; private set; }

    public string PollId => PollContent.PollId;
    public string Question => PollContent.Question;
    public List<PollOptionViewModel> Options { get; private set; }
    public int TotalVotes => PollContent.TotalVotes;
    public string TotalVotesText => $"{PollContent.TotalVoters}명 참여";
    public bool IsExpired => PollContent.IsExpired;
    public bool HasVoted => PollContent.MyVotedOptionIndices != null && PollContent.MyVotedOptionIndices.Count > 0;
    public bool CanVote => !IsExpired;
    public bool ShowResultsButton => HasVoted || IsExpired;

    public string ExpiresAtText
    {
        get
        {
            if (PollContent.ExpiresAt == null) return "마감 없음";
            if (IsExpired) return "마감됨";

            var remaining = PollContent.ExpiresAt.Value - DateTime.UtcNow;
            if (remaining.TotalDays >= 1) return $"{remaining.Days}일 남음";
            if (remaining.TotalHours >= 1) return $"{remaining.Hours}시간 남음";
            if (remaining.TotalMinutes >= 1) return $"{remaining.Minutes}분 남음";
            return "곧 마감";
        }
    }

    public void Update(PollContent pollContent, string postId)
    {
        _postId = postId;
        PollContent = pollContent;
        UpdateOptions();
    }

    private void UpdateOptions()
    {
        Options = [.. PollContent.Options.Select((option, index) => new PollOptionViewModel(option, index, PollContent.MyVotedOptionIndices?.Contains(index) ?? false, this))];
        OnPropertyChanged(nameof(Options));
    }

    public async Task VoteAsync(int optionIndex)
    {
        if (IsExpired) return;

        List<int> selectedIndices;

        if (PollContent.AllowMultipleSelection)
        {
            // Toggle selection for multiple choice polls.
            selectedIndices = PollContent.MyVotedOptionIndices?.ToList() ?? [];
            if (!selectedIndices.Remove(optionIndex)) selectedIndices.Add(optionIndex);

            if (selectedIndices.Count == 0) return;
        }
        else selectedIndices = [optionIndex];

        var result = await App.ExecuteRequestAsync(new VotePoll(_postId, PollId, selectedIndices));
        if (result.IsSuccess)
        {
            // Update poll content from the response and notify the post update.
            var updatedPollContent = result.Value.Contents.OfType<PollContent>().FirstOrDefault(content => content.PollId == PollId);
            if (updatedPollContent != null)
            {
                PollContent = updatedPollContent;
                UpdateOptions();
            }

            WeakReferenceMessenger.Default.Send(new ValueChangedMessage<PostResponseDto>(result.Value));
        }
    }

    [RelayCommand]
    private void ViewResults()
    {
        // TODO: 결과보기 페이지(PollResultsPage)가 구현되면 해당 페이지로 이동
    }
}