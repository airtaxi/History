using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using History.Commons.Api.Post;
using History.Commons.DataTypes.ResponseDtos;
using History.MobileClient.ViewModels;

#if IOS
using History.MobileClient.Helpers;
#endif

namespace History.MobileClient.Pages;

public partial class PollVotersPage : ContentPage
{
    private readonly string _postId;
    private readonly string _pollId;
    private readonly int _optionIndex;
    private readonly string _optionText;

    public PollVotersPage(string postId, string pollId, int optionIndex, string optionText)
    {
        InitializeComponent();
        _postId = postId;
        _pollId = pollId;
        _optionIndex = optionIndex;
        _optionText = optionText;

        TitleLabel.Text = $"'{_optionText}' 투표자";
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadVotersAsync();
    }

    private async Task LoadVotersAsync()
    {
        LoadingIndicator.IsRunning = true;
        LoadingIndicator.IsVisible = true;

        try
        {
            var result = await App.ExecuteRequestAsync(new GetPollVoters(_postId, _pollId, _optionIndex));
            if (result.IsFailure)
            {
                await DisplayAlertAsync("오류", result.ErrorMessage ?? "투표자 목록을 불러올 수 없습니다.", "확인");
                await App.PopAsync();
                return;
            }

            var voters = result.Value.Select(v => new PollVoterViewModel(v)).ToList();
            VotersCollectionView.ItemsSource = voters;
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
        }
    }

    private void OnLoaded(object sender, EventArgs e)
    {
#if IOS
        AppleSwipeGestureHelper.ApplyToPage(this);
#endif
    }

    private async void OnBackImageTapped(object sender, TappedEventArgs e) => await App.PopAsync();
}
