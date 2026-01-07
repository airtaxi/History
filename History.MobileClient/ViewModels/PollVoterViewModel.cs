using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using History.Commons.DataTypes.ResponseDtos;
using History.MobileClient.Pages;

namespace History.MobileClient.ViewModels;

public partial class PollVoterViewModel : ObservableObject
{
    private readonly PollVoterResponseDto _voter;

    public string UserId => _voter.User.UserId;
    public string Nickname => _voter.User.Nickname;
    public string ProfileImageUri => Utils.GenerateMediaUri(_voter.User.ProfileThumbnailMediaId);
    public string VotedAtText => Utils.GenerateFriendlyTimestamp(_voter.VotedAt, null);

    public PollVoterViewModel(PollVoterResponseDto voter)
    {
        _voter = voter;
    }

    [RelayCommand]
    private async Task NavigateToProfileAsync()
    {
        await App.PushAsync(new UserPage(UserId));
    }
}
