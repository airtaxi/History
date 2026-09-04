using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using History.Commons.Api.Post;
using History.Commons.DataTypes.Contents;
using History.Commons.DataTypes.ResponseDtos;
using History.WindowsClient.Dialogs;
using Microsoft.UI.Xaml.Controls;

namespace History.WindowsClient.ViewModels;

// Poll card surface. The control owns a single instance and pushes data in
// through the Update method.
public sealed partial class PollContentViewModel : BaseViewModel
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

        var result = await ExecuteRequestAsync(new VotePoll(_postId, PollId, selectedIndices));
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
    private async Task ViewResultsAsync()
    {
        var dialog = new PollResultsDialog(this);
        await ShowContentDialogAsync(dialog);
    }

    // Shows the voters dialog for the given option. The dialog's primary "목록 보기" button
    // returns to the results dialog; DialogHelper hides the previous dialog automatically.
    // The re-show is intentionally not awaited: awaiting it would keep the option's voters
    // command running until the re-shown results dialog closes, leaving its button disabled.
    [RelayCommand]
    public async Task ShowVotersAsync(int optionIndex)
    {
        var dialog = new PollVotersDialog(new PollVotersViewModel(this, _postId, PollId, optionIndex, PollContent.Options[optionIndex].Text));
        var result = await ShowContentDialogAsync(dialog);
        if (result == ContentDialogResult.Primary) _ = ViewResultsAsync();
    }
}