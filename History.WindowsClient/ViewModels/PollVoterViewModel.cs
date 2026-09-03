using CommunityToolkit.Mvvm.ComponentModel;
using History.Commons;
using History.Commons.DataTypes.ResponseDtos;
using History.WindowsClient.Helpers;
using Microsoft.UI.Xaml.Media.Imaging;

namespace History.WindowsClient.ViewModels;

// Wraps a poll voter entry (user + voted-at) for the voters dialog rows.
public sealed class PollVoterViewModel(PollVoterResponseDto voter)
{
    public string UserId => voter.User.UserId;
    public string Nickname => voter.User.Nickname;
    public BitmapImage ProfileImageSource => voter.User.ProfileThumbnailMediaId == null ? null : new BitmapImage(new Uri(CommonUtils.GenerateMediaUri(voter.User.ProfileThumbnailMediaId)));
    public string VotedAtText => PostHelper.GenerateFriendlyTimestamp(voter.VotedAt, null);
}