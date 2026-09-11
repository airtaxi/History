using History.Commons;

namespace History.WindowsClient.Helpers;

// Gates the unofficial Kakao Story surfaces behind the hidden unlock. The flag is stored in
// the shared settings file, so the unlock survives across restarts.
public static class KakaoStoryFeatureGateHelper
{
    private const string FeaturesEnabledKey = "KakaoStoryFeaturesEnabled";

    public static bool IsEnabled => Configuration.GetValue<bool?>(FeaturesEnabledKey) ?? false;

    public static void Enable() => Configuration.SetValue(FeaturesEnabledKey, true);

    // Clears a Kakao Story account mode left over from before the gate closed, so a locked
    // feature set cannot restore the Kakao Story mode at startup.
    public static void NormalizeStoredMode()
    {
        if (!IsEnabled)
        {
            CommonShared.LastUsedKakaoStoryMode = false;
        }
    }
}
