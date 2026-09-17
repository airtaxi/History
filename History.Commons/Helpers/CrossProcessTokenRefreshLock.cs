namespace History.Commons.Helpers;

// Serializes token refreshes across the client processes on one machine. The server revokes a
// refresh token the moment it is spent, so two processes refreshing at the same time would leave
// one of them holding a dead token and sign the user out. A file lock is used instead of a named
// mutex because a mutex must be released by the thread that acquired it, and the refresh
// continuation can resume on a different thread. Non-Windows platforms get a no-op.
public static class CrossProcessTokenRefreshLock
{
    private const string LockFileName = "token-refresh.lock";
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(100);

    // Returns a disposable lock, or null when the lock could not be acquired in time. Callers
    // proceed without the lock in that case: a rare race is better than failing the request.
    public static IDisposable Acquire(TimeSpan timeout)
    {
        if (!OperatingSystem.IsWindows()) return null;

        var lockFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "History", LockFileName);
        var deadline = DateTime.UtcNow + timeout;
        while (true)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(lockFilePath));
                return new FileStream(lockFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 1, FileOptions.DeleteOnClose);
            }
            catch (IOException)
            {
                // Another process holds the lock; wait and retry until the deadline.
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }

            if (DateTime.UtcNow >= deadline) return null;
            Thread.Sleep(RetryDelay);
        }
    }
}
