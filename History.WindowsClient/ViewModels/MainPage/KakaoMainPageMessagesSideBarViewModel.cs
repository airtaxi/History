using History.Commons.KakaoStory;
using History.WindowsClient.Helpers;

namespace History.WindowsClient.ViewModels.MainPage;

public partial class KakaoMainPageMessagesSideBarViewModel(MainPageViewModel baseViewModel) : BaseMainPageMessagesSideBarViewModel(baseViewModel)
{
    private readonly SemaphoreSlim _fetchSemaphore = new(1, 1);

    protected override HistoryWriteMessageDialogViewModel CreateWriteMessageDialogViewModel() => new KakaoWriteMessageDialogViewModel(BaseViewModel);

    public override async Task RefreshAsync()
    {
        if (_fetchSemaphore.CurrentCount == 0) return;
        await _fetchSemaphore.WaitAsync();

        try
        {
            if (!await KakaoStoryUtils.EnsureLoggedInAsync(BaseViewModel))
            {
                Items = [];
                IsEmpty = true;
                return;
            }

            var mails = await BaseViewModel.ExecuteWithLoadingAsync(() => KakaoStoryApiHandler.GetMails());
            if (mails == null)
            {
                Items = [];
                IsEmpty = true;
                return;
            }

            Items = new(mails.OrderByDescending(mail => mail.created_at).Select(mail => (BaseMessageViewModel)new KakaoMessageViewModel(mail, BaseViewModel)));
            IsEmpty = Items.Count == 0;
        }
        finally { _fetchSemaphore.Release(); }
    }
}
