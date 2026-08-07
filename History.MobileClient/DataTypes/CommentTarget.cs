namespace History.MobileClient.DataTypes;

public class CommentTarget(string userId, string nickname)
{
    public string UserId { get; } = userId;
    public string Nickname { get; } = nickname;
}
