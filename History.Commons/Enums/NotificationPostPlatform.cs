namespace History.Commons.Enums;

// Which platform a post belongs to when the Windows client and the background notification
// service exchange the post currently open in the client.
public enum NotificationPostPlatform
{
    None = 0,
    History = 1,
    KakaoStory = 2
}
