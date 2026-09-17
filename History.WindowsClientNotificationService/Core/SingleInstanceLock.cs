namespace History.WindowsClientNotificationService.Core;

// Keeps a single polling instance per user session. The mutex is never released explicitly: the
// handle closing on process exit frees it, and a crashed owner surfaces as an abandoned mutex on
// the next start instead of blocking the service forever.
public sealed class SingleInstanceLock : IDisposable
{
    private const string MutexName = @"Local\History.WindowsClientNotificationService";

    private readonly Mutex _mutex;

    private SingleInstanceLock(Mutex mutex) => _mutex = mutex;

    public static SingleInstanceLock TryAcquire()
    {
        var mutex = new Mutex(false, MutexName);
        try
        {
            if (mutex.WaitOne(TimeSpan.Zero)) return new SingleInstanceLock(mutex);
        }
        catch (AbandonedMutexException)
        {
            return new SingleInstanceLock(mutex);
        }

        mutex.Dispose();
        return null;
    }

    public void Dispose() => _mutex.Dispose();
}
