using History.Commons.Api.Message;

namespace History.WindowsClient.ViewModels.MainPage;

public partial class HistoryMainPageMessagesSideBarViewModel(MainPageViewModel baseViewModel) : BaseMainPageMessagesSideBarViewModel(baseViewModel)
{
    private readonly SemaphoreSlim _fetchSemaphore = new(1, 1);

    public override async Task RefreshAsync()
    {
        if (_fetchSemaphore.CurrentCount == 0) return;
        await _fetchSemaphore.WaitAsync();

        try
        {
            var receivedResult = await BaseViewModel.ExecuteRequestAsync(new GetReceivedMessages());
            var sentResult = await BaseViewModel.ExecuteRequestAsync(new GetSentMessages());

            if (receivedResult.IsSuccess && sentResult.IsSuccess)
            {
                var allMessages = receivedResult.Value
                    .Concat(sentResult.Value)
                    .OrderByDescending(message => message.CreatedAt);

                Items = new(allMessages.Select(message => new HistoryMessageViewModel(message, BaseViewModel)));
                IsEmpty = Items.Count == 0;
            }
            else if (receivedResult.IsSuccess)
            {
                var receivedMessages = receivedResult.Value.OrderByDescending(message => message.CreatedAt);
                Items = new(receivedMessages.Select(message => new HistoryMessageViewModel(message, BaseViewModel)));
                IsEmpty = Items.Count == 0;
            }
            else if (sentResult.IsSuccess)
            {
                var sentMessages = sentResult.Value.OrderByDescending(message => message.CreatedAt);
                Items = new(sentMessages.Select(message => new HistoryMessageViewModel(message, BaseViewModel)));
                IsEmpty = Items.Count == 0;
            }
            else
            {
                Items.Clear();
                IsEmpty = true;
            }
        }
        finally { _fetchSemaphore.Release(); }
    }
}
