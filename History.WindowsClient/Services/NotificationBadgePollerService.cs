using History.Commons;
using System.Diagnostics;

namespace History.WindowsClient.Services;

// Polls the notification badge count on a fixed cadence while the user is signed in. Every
// cycle refreshes the active account mode's unread notification count through the notifications
// view model, which updates only the count driving the title bar badge: the loaded list stays
// untouched, no loading overlay appears, and a failed cycle is skipped silently.
public sealed class NotificationBadgePollerService(Func<Task> refreshUnreadCountAsync) : IDisposable
{
    private const bool IsPollLoggingEnabled = true;
    private static readonly TimeSpan s_pollInterval = TimeSpan.FromSeconds(10);

    private CancellationTokenSource _pollingCancellationTokenSource;

    public void Start()
    {
        if (_pollingCancellationTokenSource is not null) return;

        LogPoll("Notification badge polling started.");
        _pollingCancellationTokenSource = new CancellationTokenSource();
        _ = RunPollingLoopAsync(_pollingCancellationTokenSource.Token);
    }

    public void Stop()
    {
        if (_pollingCancellationTokenSource is null) return;

        LogPoll("Notification badge polling stopped.");
        _pollingCancellationTokenSource.Cancel();
        _pollingCancellationTokenSource.Dispose();
        _pollingCancellationTokenSource = null;
    }

    public void Dispose() => Stop();

    private async Task RunPollingLoopAsync(CancellationToken cancellationToken)
    {
        // A per-loop timer keeps a restarting loop from sharing a timer with a
        // still-finishing previous loop (PeriodicTimer only allows one waiter).
        using var periodicTimer = new PeriodicTimer(s_pollInterval);
        try
        {
            while (await periodicTimer.WaitForNextTickAsync(cancellationToken))
            {
                // Belt-and-suspenders guard: the poller is stopped on sign-out, but a
                // signed-out cycle must never issue an authenticated request.
                if (CommonShared.ApiHandler == ApiHandler.Public) continue;

                try { await refreshUnreadCountAsync(); }
                catch (Exception exception) { LogPoll($"Notification badge poll cycle failed: {exception.Message}"); }
            }
        }
        catch (OperationCanceledException) { }
    }

    private static void LogPoll(string message)
    {
        if (!IsPollLoggingEnabled) return;
        Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
    }
}
