namespace History.MobileClient.DataTypes;

public class CommentTarget
{
    public string UserId { get; }
    public string Nickname { get; }

    public CommentTarget(string userId, string nickname)
    {
        UserId = userId;
        Nickname = nickname;
    }
}
