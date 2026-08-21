namespace History.Commons.DataTypes.RequestDtos;

/// <summary>
/// DTO for uploading the user's Kakao Story KAuth id token to the server so the
/// server-side polling service can fetch notifications on their behalf.
/// Tokens are kept in memory only and never persisted. The notification filter
/// flags mirror the client's settings page; the master toggle is not included
/// because turning it off deletes the session (DeleteKakaoStoryToken) instead.
/// </summary>
public class UpdateKakaoStoryTokenRequestDto
{
    public string IdToken { get; set; }

    public bool IsFavoriteFriendNotificationEnabled { get; set; } = true;

    public bool IsEmotionNotificationEnabled { get; set; } = true;
}
