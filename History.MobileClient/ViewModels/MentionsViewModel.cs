using CommunityToolkit.Mvvm.ComponentModel;
using SpeakLink.Mention;

namespace History.MobileClient.ViewModels;

public partial class MentionsViewModel : ObservableObject
{
    public event EventHandler<string> ImageInputRequested;
    public MentionsViewModel()
    {
        MentionSearchCommand = new Command<MentionSearchEventArgs>(OnMentionSearch);
        ImageInputCommand = new Command(OnImageInput);
    }

    private void OnImageInput(object obj)
    {
        if (obj is string imagePath)
        {
            ImageInputRequested?.Invoke(this, imagePath);
        }
    }

    [ObservableProperty]
    public partial List<MentionViewModel> ViewModels { get; set; }

    [ObservableProperty]
    public partial bool IsDisplayingMentions { get; set; }

    public Command<MentionSearchEventArgs> MentionSearchCommand { get; }
    public Command ImageInputCommand { get; }

    private void OnMentionSearch(MentionSearchEventArgs mentionSearchEventArgs)
    {
        List<MentionViewModel> viewModels;
        var query = mentionSearchEventArgs.MentionQuery.Trim();
        if (string.IsNullOrEmpty(query)) viewModels = [.. Shared.Friends.Select(x => new MentionViewModel(x))];
        else viewModels = [.. Shared.Friends
                .Where(x => x.Handle.Contains(query, StringComparison.InvariantCultureIgnoreCase)
                    || x.Nickname.Contains(query, StringComparison.InvariantCultureIgnoreCase))
                .Select(x => new MentionViewModel(x))];
        ViewModels = viewModels;
        IsDisplayingMentions = viewModels.Count > 0;
    }
}
