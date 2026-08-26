namespace History.ApiService;

public class Constants
{
    public const string JwtIssuer = "History";
    public const string JwtAudience = "https://api.history.cenox.io";
    public static string JwtKey => Environment.GetEnvironmentVariable("HISTORY_JWT_KEY") ?? throw new InvalidOperationException("Environment variable 'HISTORY_JWT_KEY' is required.");
    public const string AutoFriendUserId = "106735740295566028473";
}