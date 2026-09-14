using History.Commons;
using System.Diagnostics;

namespace History.WindowsClient.Services;

// Polls the badge counts on a fixed cadence while the user is signed in. Every cycle runs the
// registered badge refresh targets, each of which updates only the counts driving the title bar
// badge and the main page badges: the loaded lists stay untouched, no loading overlay appears,
// and a failed target is skipped silently.
public sealed class BadgePollerService : IDisposable
{
    private static readonly bool s_isPollLoggingEnabled = true;
    private static readonly TimeSpan s_pollInterval = TimeSpan.FromSeconds(10);

    private readonly List<Func<Task>> _refreshTargets = [];
    private readonly SemaphoreSlim _pollSemaphore = new(1, 1);
    private CancellationTokenSource _pollingCancellationTokenSource;

    // Registers a badge refresh target; every target runs once per poll cycle.
    public void AddRefreshTarget(Func<Task> refreshAsync) => _refreshTargets.Add(refreshAsync);

    public void Start()
    {
        if (_pollingCancellationTokenSource is not null) return;

        LogPoll("Badge polling started.");
        _pollingCancellationTokenSource = new CancellationTokenSource();
        // Polls once immediately so the badges reflect the current state right after login.
        _ = PollOnceAsync();
        _ = RunPollingLoopAsync(_pollingCancellationTokenSource.Token);
    }

    public void Stop()
    {
        if (_pollingCancellationTokenSource is null) return;

        LogPoll("Badge polling stopped.");
        _pollingCancellationTokenSource.Cancel();
        _pollingCancellationTokenSource.Dispose();
        _pollingCancellationTokenSource = null;
    }

    public void Dispose() => Stop();

    private async Task PollOnceAsync()
    {
        if (_pollSemaphore.CurrentCount == 0) return;

        try
        {
            await _pollSemaphore.WaitAsync();

            // Belt-and-suspenders guard: the poller is stopped on sign-out, but a
            // signed-out cycle must never issue an authenticated request.
            if (CommonShared.ApiHandler == ApiHandler.Public) return;

            foreach (var refreshTarget in _refreshTargets)
            {
                try { await refreshTarget(); }
                catch (Exception exception) { LogPoll($"Badge poll target failed: {exception.Message}"); }
            }
        }
        finally { _pollSemaphore.Release(); }
    }

    private async Task RunPollingLoopAsync(CancellationToken cancellationToken)
    {
        // A per-loop timer keeps a restarting loop from sharing a timer with a
        // still-finishing previous loop (PeriodicTimer only allows one waiter).
        using var periodicTimer = new PeriodicTimer(s_pollInterval);
        try
        {
            while (await periodicTimer.WaitForNextTickAsync(cancellationToken))
            {
                LogPoll("Badge poll cycle.");
                try { await PollOnceAsync(); }
                catch (Exception exception) { LogPoll($"Badge poll cycle failed: {exception.Message}"); }
            }
        }
        catch (OperationCanceledException) { }
    }

    private static void LogPoll(string message)
    {
        if (!s_isPollLoggingEnabled) return;
        Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
    }
}
