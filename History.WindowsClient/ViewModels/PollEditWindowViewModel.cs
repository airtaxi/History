using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using History.Commons.DataTypes.Contents;
using History.WindowsClient.Models;

namespace History.WindowsClient.ViewModels;

// Poll composing state for PollEditWindow. Confirm validates the definition and hands the
// built PollContent to the opener through the Confirmed event; the dialog events are
// fulfilled by the window code-behind.
public sealed partial class PollEditWindowViewModel : BaseViewModel
{
    private const int MinOptionCount = 2;

    [ObservableProperty]
    public partial string Title { get; set; }

    [ObservableProperty]
    public partial bool AllowMultipleSelection { get; set; }

    // The toggle arms the expiration pickers; turning it off clears any partially picked
    // date/time so an old selection cannot silently come back when it is re-enabled.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ExpirationDateTime))]
    public partial bool IsExpirationEnabled { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ExpirationDateTime))]
    public partial DateTimeOffset? ExpirationDate { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ExpirationDateTime))]
    public partial TimeSpan? ExpirationTime { get; set; }

    public ObservableCollection<PollEditOptionItemViewModel> Options { get; } = [];

    public DateTime? ExpirationDateTime => IsExpirationEnabled && ExpirationDate is { } date && ExpirationTime is { } time ? date.LocalDateTime.Date + time : null;

    public event EventHandler<PollContent> Confirmed;

    public PollEditWindowViewModel()
    {
        for (var index = 0; index < MinOptionCount; index++)
        {
            AddOption();
        }
    }

    partial void OnIsExpirationEnabledChanged(bool value)
    {
        if (!value)
        {
            ExpirationDate = null;
            ExpirationTime = null;
        }
    }

    [RelayCommand]
    private void AddOption()
    {
        var option = new PollEditOptionItemViewModel(this) { PlaceholderText = $"옵션 {Options.Count + 1}" };
        Options.Add(option);
    }

    // Removes the row and renumbers the remaining placeholders so they stay in order.
    // The minimum count is protected: deleting at the floor shows a message instead.
    public async Task RemoveOptionAsync(PollEditOptionItemViewModel option)
    {
        if (Options.Count <= MinOptionCount)
        {
            await ShowMessageDialogAsync(new MessageDialogParameters("투표 생성", $"투표 선택지는 최소 {MinOptionCount}개 이상이어야 합니다."));
            return;
        }

        if (!Options.Remove(option)) return;

        for (var index = 0; index < Options.Count; index++) Options[index].PlaceholderText = $"옵션 {index + 1}";
    }

    // Builds the poll content after validation: title required, at least two non-empty and
    // distinct options, and a future expiration when the toggle is on. The expiration is
    // stored as UTC because expiration checks compare against UtcNow.
    [RelayCommand]
    private async Task ConfirmAsync()
    {
        var title = Title?.Trim();
        if (string.IsNullOrEmpty(title))
        {
            await ShowMessageDialogAsync(new MessageDialogParameters("투표 생성", "투표 제목을 입력해주세요."));
            return;
        }

        var optionTexts = Options.Select(option => option.Text?.Trim()).ToList();
        if (optionTexts.Count < MinOptionCount || optionTexts.Any(string.IsNullOrWhiteSpace))
        {
            await ShowMessageDialogAsync(new MessageDialogParameters("투표 생성", $"투표 선택지는 최소 {MinOptionCount}개 이상 입력해야 합니다."));
            return;
        }

        if (optionTexts.Distinct().Count() != optionTexts.Count)
        {
            await ShowMessageDialogAsync(new MessageDialogParameters("투표 생성", "중복된 선택지가 있습니다."));
            return;
        }

        if (IsExpirationEnabled && (ExpirationDateTime is not { } expiration || expiration <= DateTime.Now))
        {
            await ShowMessageDialogAsync(new MessageDialogParameters("투표 생성", "투표 종료 시각은 현재 시각 이후로 설정해야 합니다."));
            return;
        }

        Confirmed?.Invoke(this, new PollContent
        {
            PollId = Guid.NewGuid().ToString("N"),
            Question = title,
            AllowMultipleSelection = AllowMultipleSelection,
            ExpiresAt = ExpirationDateTime?.ToUniversalTime(),
            Options = [.. optionTexts.Select(optionText => new PollOption { Text = optionText })]
        });
    }
}
