using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using History.Commons.Api.Post;
using History.Commons.DataTypes.Contents;
using History.MobileClient.ViewModels;
using System.Collections.ObjectModel;

namespace History.MobileClient.Pages;

public partial class PollResultsPage : ContentPage
{
    private readonly string _postId;
    private readonly string _pollId;
    private PollContent _pollContent;

    public PollResultsPage(string postId, string pollId)
    {
        InitializeComponent();
        _postId = postId;
        _pollId = pollId;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadPollResultsAsync();
    }

    private async Task LoadPollResultsAsync()
    {
        LoadingIndicator.IsRunning = true;
        LoadingIndicator.IsVisible = true;

        try
        {
            var result = await App.ExecuteRequestAsync(new GetPost(_postId));
            if (result.IsFailure)
            {
                await DisplayAlertAsync("오류", "투표 정보를 불러올 수 없습니다.", "확인");
                await App.PopAsync();
                return;
            }

            _pollContent = result.Value.Contents.OfType<PollContent>().FirstOrDefault(p => p.PollId == _pollId);
            if (_pollContent == null)
            {
                await DisplayAlertAsync("오류", "투표를 찾을 수 없습니다.", "확인");
                await App.PopAsync();
                return;
            }

            UpdateUI();
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
        }
    }

    private void UpdateUI()
    {
        QuestionLabel.Text = _pollContent.Question;
        TotalVotesLabel.Text = $"총 {_pollContent.TotalVotes}명 참여";

        if (_pollContent.ExpiresAt == null) ExpiresAtLabel.Text = "마감 없음";
        else if (_pollContent.IsExpired) ExpiresAtLabel.Text = "마감됨";
        else
        {
            var remaining = _pollContent.ExpiresAt.Value - DateTime.UtcNow;
            if (remaining.TotalDays >= 1) ExpiresAtLabel.Text = $"{remaining.Days}일 남음";
            else if (remaining.TotalHours >= 1) ExpiresAtLabel.Text = $"{remaining.Hours}시간 남음";
            else if (remaining.TotalMinutes >= 1) ExpiresAtLabel.Text = $"{remaining.Minutes}분 남음";
            else ExpiresAtLabel.Text = "곧 마감";
        }

        var options = _pollContent.Options
            .Select((opt, index) => new PollResultOptionViewModel(
                opt.Text,
                opt.VoteCount,
                _pollContent.TotalVotes,
                index,
                _postId,
                _pollId))
            .ToList();

        OptionsCollectionView.ItemsSource = options;
    }

    private async void OnBackImageTapped(object sender, TappedEventArgs e) => await App.PopAsync();
}