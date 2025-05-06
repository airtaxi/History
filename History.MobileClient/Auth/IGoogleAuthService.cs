namespace History.MobileClient.Auth;

public interface IGoogleAuthService
{
    Task<string> AuthenticateAsync();
    Task<bool> SignOutAsync();
}
