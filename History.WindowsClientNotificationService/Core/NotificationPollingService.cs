using History.WindowsClientNotificationService.Notifications;
using History.Commons.Ipc;
using History.WindowsClientNotificationService.Polling;
using History.WindowsClientNotificationService.State;

namespace History.WindowsClientNotificationService.Core;

// Owns the two polling loops: History notifications every few seconds and Kakao Story at a lower
// cadence, because the service talks to story.kakao.com directly and one desktop IP must not look
// like it is abusing the endpoint. A client process can signal the stop event to end the loops.
public sealed class NotificationPollingService : IAsyncDisposable
{
    private static readonly TimeSpan HistoryPollInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan KakaoStoryPollInterval = TimeSpan.FromSeconds(10);

    private readonly FileLogger _logger;
    private readonly NotificationStateStore _stateStore;
    private readonly ClientPostDisplayBridge _clientPostDisplayBridge;
    private readonly HistoryNotificationPoller _historyNotificationPoller;
    private readonly KakaoStoryNotificationPoller _kakaoStoryNotificationPoller;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly EventWaitHandle _stopEvent = new(false, EventResetMode.ManualReset, NotificationServiceProtocol.StopEventName);

    private RegisteredWaitHandle _stopRegistration;

    public NotificationPollingService(FileLogger logger)
    {
        _logger = logger;
        _stateStore = new NotificationStateStore(logger);
        var toastPublisher = new ToastPublisher(logger);
        _clientPostDisplayBridge = new ClientPostDisplayBridge(logger, toastPublisher);
        _clientPostDisplayBridge.Start();
        _historyNotificationPoller = new HistoryNotificationPoller(_stateStore, toastPublisher, _clientPostDisplayBridge, logger);
        _kakaoStoryNotificationPoller = new KakaoStoryNotificationPoller(_stateStore, toastPublisher, _clientPostDisplayBridge, logger);
    }

    public async Task RunAsync()
    {
        _logger.Log("Notification service started.");
        _stopRegistration = ThreadPool.RegisterWaitForSingleObject(_stopEvent, (_, _) => _cancellationTokenSource.Cancel(), null, Timeout.Infinite, executeOnlyOnce: true);

        await Task.WhenAll(RunPollingLoopAsync(HistoryPollInterval, _historyNotificationPoller.PollOnceAsync), RunPollingLoopAsync(KakaoStoryPollInterval, _kakaoStoryNotificationPoller.PollOnceAsync));
        _logger.Log("Notification service stopped.");
    }

    public async Task RunOnceAsync()
    {
        await _historyNotificationPoller.PollOnceAsync();
        await _kakaoStoryNotificationPoller.PollOnceAsync();
        _stateStore.Save();
        _logger.Log("Single poll cycle finished.");
    }

    private async Task RunPollingLoopAsync(TimeSpan interval, Func<Task> pollAsync)
    {
        using var periodicTimer = new PeriodicTimer(interval);
        while (true)
        {
            try { await pollAsync(); }
            catch (OperationCanceledException) { return; }
            catch (Exception exception) { _logger.Log($"Poll cycle failed: {exception}"); }

            try
            {
                if (!await periodicTimer.WaitForNextTickAsync(_cancellationTokenSource.Token)) return;
            }
            catch (OperationCanceledException) { return; }
        }
    }

    public ValueTask DisposeAsync()
    {
        _stopRegistration?.Unregister(null);
        _cancellationTokenSource.Cancel();
        _stateStore.Save();
        _clientPostDisplayBridge.Dispose();
        _cancellationTokenSource.Dispose();
        _stopEvent.Dispose();
        return ValueTask.CompletedTask;
    }
}
