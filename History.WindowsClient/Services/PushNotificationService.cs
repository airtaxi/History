using History.Commons;
using History.Commons.Api.PushNotification;
using Windows.Networking.PushNotifications;
using Windows.Storage;

namespace History.WindowsClient.Services;

// Manages the WNS channel for this device: requests a fresh channel on every app launch
// (WNS channels expire after 30 days), keeps the server registration current, and removes
// the channel when the user signs out.
public sealed class PushNotificationService
{
    private const string WnsChannelUriSettingKey = "WnsChannelUri";
    private const string WnsChannelRegisteredSettingKey = "WnsChannelRegistered";
    private const int MaxChannelRequestAttempts = 3;
    private static readonly TimeSpan ChannelRequestRetryDelay = TimeSpan.FromSeconds(10);

    private readonly SemaphoreSlim _channelLock = new(1, 1);

    // Registers the current WNS channel with the server unless it is unchanged since the last
    // successful registration. Best-effort: failures are swallowed and retried on the next app
    // launch, following the MSDN channel guidance of re-requesting a fresh channel each run.
    public async Task InitializeAsync()
    {
        await _channelLock.WaitAsync();
        try
        {
            var channelUri = await RequestChannelAsync();
            if (channelUri == null) return;

            var localSettings = ApplicationData.Current.LocalSettings;
            var previousChannelUri = localSettings.Values[WnsChannelUriSettingKey] as string;
            var isRegistered = localSettings.Values[WnsChannelRegisteredSettingKey] is bool registered && registered;
            if (previousChannelUri == channelUri && isRegistered) return;

            if (await CommonShared.ApiHandler.TryExecuteRequestAsync(new RegisterWnsChannel(channelUri)))
            {
                localSettings.Values[WnsChannelUriSettingKey] = channelUri;
                localSettings.Values[WnsChannelRegisteredSettingKey] = true;
            }
        }
        catch { } // Channel registration is best-effort; the next app launch retries.
        finally { _channelLock.Release(); }
    }

    // Removes the registered channel from the server and forgets the local channel state.
    // TODO: Call this from the logout flow once the Windows client has one.
    public async Task UnregisterAsync()
    {
        await _channelLock.WaitAsync();
        try
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values[WnsChannelUriSettingKey] is not string channelUri || string.IsNullOrEmpty(channelUri)) return;

            await CommonShared.ApiHandler.TryExecuteRequestAsync(new DeleteWnsChannel(channelUri));
            localSettings.Values.Remove(WnsChannelUriSettingKey);
            localSettings.Values.Remove(WnsChannelRegisteredSettingKey);
        }
        finally { _channelLock.Release(); }
    }

    // WNS recommends three attempts with a ten-second delay between each, because the channel
    // request can fail when the device has no data connection.
    private static async Task<string> RequestChannelAsync()
    {
        for (var attempt = 1; attempt <= MaxChannelRequestAttempts; attempt++)
        {
            try
            {
                var channel = await PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync();
                return channel?.Uri;
            }
            catch
            {
                if (attempt == MaxChannelRequestAttempts) return null;
                await Task.Delay(ChannelRequestRetryDelay);
            }
        }

        return null;
    }
}