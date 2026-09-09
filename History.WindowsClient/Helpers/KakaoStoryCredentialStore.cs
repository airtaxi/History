using System.Diagnostics;
using Windows.Security.Credentials;

namespace History.WindowsClient.Helpers;

// Stores the Kakao Story login credentials (email + password) in the Windows
// Credential Locker (PasswordVault) instead of the plain settings.json file. The
// password is never persisted in plain text anywhere on the device.
public static class KakaoStoryCredentialStore
{
    private const string ResourceName = "History.KakaoStory";
    private const string EmailKey = "KakaoStoryEmail";
    private const string PasswordKey = "KakaoStoryPassword";

    public static async Task<string> GetEmailAsync() => await GetCredentialAsync(EmailKey);

    public static async Task<string> GetPasswordAsync() => await GetCredentialAsync(PasswordKey);

    public static async Task SaveAsync(string email, string password)
    {
        try
        {
            await Task.Run(() =>
            {
                var vault = new PasswordVault();
                DeleteCredential(vault, EmailKey);
                DeleteCredential(vault, PasswordKey);
                vault.Add(new PasswordCredential(ResourceName, EmailKey, email));
                vault.Add(new PasswordCredential(ResourceName, PasswordKey, password));
            });
        }
        catch (Exception exception) { Debug.WriteLine($"Kakao Story credential save failed: {exception.Message}"); }
    }

    public static void Clear()
    {
        try
        {
            var vault = new PasswordVault();
            DeleteCredential(vault, EmailKey);
            DeleteCredential(vault, PasswordKey);
        }
        catch (Exception exception) { Debug.WriteLine($"Kakao Story credential clear failed: {exception.Message}"); }
    }

    private static async Task<string> GetCredentialAsync(string key)
    {
        try
        {
            return await Task.Run(() =>
            {
                var vault = new PasswordVault();
                foreach (var credential in vault.RetrieveAll().Where(credential => credential.Resource == ResourceName && credential.UserName == key))
                {
                    credential.RetrievePassword();
                    return credential.Password;
                }
                return null;
            });
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"Kakao Story credential load failed: {exception.Message}");
            return null;
        }
    }

    private static void DeleteCredential(PasswordVault vault, string key)
    {
        try
        {
            foreach (var credential in vault.RetrieveAll().Where(credential => credential.Resource == ResourceName && credential.UserName == key))
            {
                vault.Remove(credential);
            }
        }
        catch { }
    }
}