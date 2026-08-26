using History.Commons;
using Microsoft.Maui.Storage;

namespace History.MobileClient.Helpers;

/// <summary>
/// Stores the Kakao Story login credentials (email + password) in the platform
/// secure storage (Android Keystore-backed EncryptedSharedPreferences / iOS
/// Keychain) instead of the plain settings.json file. The password is never
/// persisted in plain text anywhere on the device.
/// </summary>
public static class KakaoStoryCredentialStore
{
    private const string EmailKey = "KakaoStoryEmail";
    private const string PasswordKey = "KakaoStoryPassword";

    private static bool s_legacyCleanupAttempted;

    public static async Task<string> GetEmailAsync()
    {
        ClearLegacyCredentialsIfPresent();
        try { return await SecureStorage.Default.GetAsync(EmailKey); }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine($"Kakao Story email load failed: {exception.Message}");
            return null;
        }
    }

    public static async Task<string> GetPasswordAsync()
    {
        ClearLegacyCredentialsIfPresent();
        try { return await SecureStorage.Default.GetAsync(PasswordKey); }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine($"Kakao Story password load failed: {exception.Message}");
            return null;
        }
    }

    public static async Task SaveAsync(string email, string password)
    {
        try
        {
            await SecureStorage.Default.SetAsync(EmailKey, email);
            await SecureStorage.Default.SetAsync(PasswordKey, password);
        }
        catch (Exception exception) { System.Diagnostics.Debug.WriteLine($"Kakao Story credential save failed: {exception.Message}"); }
    }

    public static void Clear()
    {
        SecureStorage.Default.Remove(EmailKey);
        SecureStorage.Default.Remove(PasswordKey);
        Configuration.SetValue("KakaoStoryEmail", null);
        Configuration.SetValue("KakaoStoryPassword", null);
    }

    /// <summary>
    /// Removes the legacy settings.json credentials (stored AES-encrypted with
    /// a hardcoded key by older versions) so the caller sees no saved credential
    /// and the user is prompted to re-enter them, which then saves into secure
    /// storage.
    /// </summary>
    public static void ClearLegacyCredentialsIfPresent()
    {
        if (s_legacyCleanupAttempted) return;
        s_legacyCleanupAttempted = true;

        if (Configuration.GetValue<string>("KakaoStoryEmail") != null || Configuration.GetValue<string>("KakaoStoryPassword") != null)
        {
            Configuration.SetValue("KakaoStoryEmail", null);
            Configuration.SetValue("KakaoStoryPassword", null);
        }
    }
}
