using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using History.Commons.Api.Post;
using History.Commons.DataTypes.Contents;
using History.Commons.DataTypes.ResponseDtos;

namespace History.MobileClient.ViewModels;

public partial class PollContentViewModel : ObservableObject, IContentViewModel
{
    private readonly string _postId;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Question))]
    [NotifyPropertyChangedFor(nameof(Options))]
    [NotifyPropertyChangedFor(nameof(TotalVotes))]
    [NotifyPropertyChangedFor(nameof(TotalVotesText))]
    [NotifyPropertyChangedFor(nameof(IsExpired))]
    [NotifyPropertyChangedFor(nameof(ExpiresAtText))]
    [NotifyPropertyChangedFor(nameof(HasVoted))]
    [NotifyPropertyChangedFor(nameof(CanVote))]
    public partial PollContent PollContent { get; set; }

    public string PollId => PollContent.PollId;
    public string Question => PollContent.Question;
    public List<PollOptionViewModel> Options { get; private set; }
    public int TotalVotes => PollContent.TotalVotes;
    public string TotalVotesText => $"{TotalVotes}명 참여";
    public bool IsExpired => PollContent.IsExpired;
    public bool HasVoted => PollContent.MyVotedOptionIndices != null && PollContent.MyVotedOptionIndices.Count > 0;
    public bool CanVote => !IsExpired;

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

    public PollContentViewModel(PollContent pollContent, string postId)
    {
        _postId = postId;
        PollContent = pollContent;
        UpdateOptions();
    }

    private void UpdateOptions()
    {
        Options = PollContent.Options
            .Select((opt, index) => new PollOptionViewModel(
                opt,
                index,
                PollContent.MyVotedOptionIndices?.Contains(index) ?? false,
                TotalVotes,
                this))
            .ToList();
        OnPropertyChanged(nameof(Options));
    }

    public async Task VoteAsync(int optionIndex)
    {
        if (IsExpired) return;

        List<int> selectedIndices;

        if (PollContent.AllowMultipleSelection)
        {
            // Toggle selection for multiple choice
            selectedIndices = PollContent.MyVotedOptionIndices?.ToList() ?? [];
            if (selectedIndices.Contains(optionIndex))
                selectedIndices.Remove(optionIndex);
            else
                selectedIndices.Add(optionIndex);

            if (selectedIndices.Count == 0) return;
        }
        else
        {
            // Single selection
            selectedIndices = [optionIndex];
        }

        var result = await App.ExecuteRequestAsync(new VotePoll(_postId, PollId, selectedIndices));
        if (result.IsSuccess)
        {
            // Update poll content from response
            var updatedPollContent = result.Value.Contents.OfType<PollContent>().FirstOrDefault(p => p.PollId == PollId);
            if (updatedPollContent != null)
            {
                PollContent = updatedPollContent;
                UpdateOptions();
            }

            // Notify post update
            WeakReferenceMessenger.Default.Send(new ValueChangedMessage<PostResponseDto>(result.Value));
        }
    }
}

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
    public double ProgressWidth => Percentage;

    public bool ShowResults => _parent.HasVoted || _parent.IsExpired;

    public PollOptionViewModel(PollOption option, int index, bool isSelected, int totalVotes, PollContentViewModel parent)
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
