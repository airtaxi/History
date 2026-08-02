namespace History.Uno.Services;

public interface IGoogleAuthService
{
    Task<string> AuthenticateAsync();
    Task<bool> SignOutAsync();
}