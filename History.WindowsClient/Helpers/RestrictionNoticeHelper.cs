using History.WindowsClient.Models;
using History.WindowsClient.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;

namespace History.WindowsClient.Helpers;

// Restriction notice and appeal flow shared by the notification list and the toast deep links:
// the appeal path copies the official Discord invite URL.
public static class RestrictionNoticeHelper
{
    public static async Task ShowAsync(BaseViewModel baseViewModel, string body)
    {
        var result = await baseViewModel.ShowMessageDialogAsync(new MessageDialogParameters("제재 내역", body, "확인", "소명 신청하기"));
        if (result != ContentDialogResult.Secondary) return;

        var copyResult = await baseViewModel.ShowMessageDialogAsync(new MessageDialogParameters("알림", "공식 디스코드에서 소명 신청을 받고 있습니다.", "디스코드 초대 URL 복사", cancelButtonText: "확인"));
        if (copyResult != ContentDialogResult.Primary) return;

        var dataPackage = new DataPackage { RequestedOperation = DataPackageOperation.Copy };
        dataPackage.SetText(Constants.DiscordInviteUrl);
        Clipboard.SetContent(dataPackage);

        await baseViewModel.ShowMessageDialogAsync(new MessageDialogParameters("알림", "디스코드 초대 URL이 클립보드에 복사되었습니다."));
    }
}
