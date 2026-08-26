using CommunityToolkit.Mvvm.ComponentModel;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using History.Commons.Enums;

namespace History.MobileClient.ViewModels;

public partial class ModerationRecordViewModel : ObservableObject
{
    public ModerationRecordResponseDto Record { get; }

    public string Id => Record.Id;
    public string TypeDisplayString => Record.Type.ToDisplayString();
    public string Reason => Record.Reason;
    public string CreatedAtDisplayString => Record.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
    public UserResponseDto User => Record.User;
    public UserResponseDto Moderator => Record.Moderator;

    public List<IContentViewModel> ContentViewModels { get; }
    public bool HasContents => ContentViewModels?.Count > 0;

    public ModerationRecordViewModel(ModerationRecordResponseDto record)
    {
        Record = record;
        ContentViewModels = record.AssociatedContents != null && record.AssociatedContents.Count > 0
            ? Utils.GenerateContentViewModels(record.AssociatedContents, PostType.Unwrapped)
            : [];
    }
}
