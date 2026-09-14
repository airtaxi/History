using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using History.Commons.DataTypes.ResponseDtos;
using Windows.ApplicationModel.DataTransfer;

namespace History.WindowsClient.ViewModels.Extras;

// Wraps an invite code entry (code + usage state) and owns the row's copy action: the
// button glyph swaps to a checkmark for two seconds as the copy confirmation.
public sealed partial class InviteCodeViewModel(InviteCodeResponseDto inviteCode) : ObservableObject
{
    // Segoe Fluent Icons glyphs for the row copy button (the template binds the glyph
    // so the feedback swap needs no view-side logic).
    private const string CopyGlyph = "\uE8C8";
    private const string CheckMarkGlyph = "\uE73E";
    private const string UnknownNickname = "알 수 없음";

    private bool _isCopyFeedbackActive;

    public string Id => inviteCode.Id;
    public string Code => inviteCode.Code;
    public bool IsActive => inviteCode.IsActive;
    public bool IsUsed => !inviteCode.IsActive && !string.IsNullOrEmpty(inviteCode.UsedByUserId);
    public string StatusText => inviteCode.IsActive ? "사용 가능" : "사용됨";

    public string UsedByText
    {
        get
        {
            var nickname = inviteCode.UsedBy?.Nickname ?? UnknownNickname;
            var usedAtText = inviteCode.UsedAt?.ToLocalTime().ToString("yyyy.MM.dd HH:mm");
            if (usedAtText == null) return $"사용자: {nickname}";
            return $"사용자: {nickname} ({usedAtText})";
        }
    }

    // Copy-feedback surface: swapped with the checkmark while the confirmation is shown.
    [ObservableProperty]
    public partial string CopyButtonGlyph { get; private set; } = CopyGlyph;

    // Copies the code and shows the checkmark feedback for two seconds; re-taps during
    // the feedback are ignored so the confirmation stays visible.
    [RelayCommand]
    private async Task HandleCopyAsync()
    {
        if (_isCopyFeedbackActive) return;

        var dataPackage = new DataPackage { RequestedOperation = DataPackageOperation.Copy };
        dataPackage.SetText(Code);
        Clipboard.SetContent(dataPackage);

        _isCopyFeedbackActive = true;
        CopyButtonGlyph = CheckMarkGlyph;
        await Task.Delay(2000);
        CopyButtonGlyph = CopyGlyph;
        _isCopyFeedbackActive = false;
    }
}
