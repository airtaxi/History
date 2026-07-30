using System.Text.Json.Serialization;

namespace History.Commons.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<InviteCodeRequestStatus>))]
public enum InviteCodeRequestStatus
{
    Pending,
    Accepted,
    Rejected
}

public static class InviteCodeRequestStatusExtensions
{
    public static string ToDisplayString(this InviteCodeRequestStatus status) => status switch
    {
        InviteCodeRequestStatus.Pending => "대기 중",
        InviteCodeRequestStatus.Accepted => "수락됨",
        InviteCodeRequestStatus.Rejected => "거부됨",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };
}