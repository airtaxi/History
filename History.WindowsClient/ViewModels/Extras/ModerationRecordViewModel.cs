using CommunityToolkit.Mvvm.ComponentModel;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using History.WindowsClient.Helpers;

namespace History.WindowsClient.ViewModels.Extras;

// Wraps a restriction record for the moderation records list: the summary fields and the
// renderable content items for the removed post/comment. Media and poll contents are dropped
// because the media files are deleted and the target post no longer exists.
public sealed partial class ModerationRecordViewModel(ModerationRecordResponseDto record, ModerationRecordsPageViewModel pageViewModel) : ObservableObject
{
    private const string UnknownNickname = "알 수 없음";
    private const string WithdrawnNickname = "탈퇴한 사용자";

    public string Id => record.Id;
    public string TypeText => record.Type.ToDisplayString();
    public string TargetText => $"대상: {record.User?.Nickname ?? WithdrawnNickname}";
    public string ModeratorText => $"처리자: {record.Moderator?.Nickname ?? UnknownNickname}";
    public string ReasonText => $"사유: {(string.IsNullOrEmpty(record.Reason) ? "없음" : record.Reason)}";
    public string CreatedAtText => record.CreatedAt.ToLocalTime().ToString("yyyy.MM.dd HH:mm");

    public List<IContentViewModel> Contents { get; } = PostHelper.GenerateContentViewModels(record.AssociatedContents, PostType.Unwrapped, pageViewModel, forModerationRecord: true);

    public bool HasContents => Contents.Count > 0;
}
