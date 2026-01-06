using CommunityToolkit.Mvvm.ComponentModel;
using History.Commons.DataTypes.ResponseDtos;
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

    public ModerationRecordViewModel(ModerationRecordResponseDto record)
    {
        Record = record;
    }
}
