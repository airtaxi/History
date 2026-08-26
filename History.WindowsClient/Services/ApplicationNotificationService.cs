using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

namespace History.WindowsClient.Services;

public sealed class ApplicationNotificationService()
{
    public void ShowStoreUpdateAvailableNotification(int availableUpdateCount, Uri storeProductPageAddress)
    {
        var notificationButton = new AppNotificationButton("스토어 열기").SetInvokeUri(storeProductPageAddress);
        ShowNotification("업데이트 가능", $"{availableUpdateCount}개의 업데이트가 Microsoft Store에서 사용 가능합니다.", notificationButton);
    }

    private static void ShowNotification(string notificationTitle, string notificationMessage, AppNotificationButton notificationButton = null)
    {
        try
        {
            if (!AppNotificationManager.IsSupported()) return;

            var appNotificationBuilder = new AppNotificationBuilder()
                .AddText(notificationTitle)
                .AddText(notificationMessage);

            if (notificationButton is not null) appNotificationBuilder.AddButton(notificationButton);

            AppNotificationManager.Default.Show(appNotificationBuilder.BuildNotification());
        }
        catch { }
    }
}