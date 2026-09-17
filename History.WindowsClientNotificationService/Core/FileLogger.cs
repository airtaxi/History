namespace History.WindowsClientNotificationService.Core;

// The exe runs without a console window, so every diagnostic goes to a size-capped file next to
// the app settings, where support can pick it up.
public sealed class FileLogger
{
    private const long MaxLogFileBytes = 1024 * 1024;
    private const string LogDirectoryName = "Logs";
    private const string LogFileName = "notification-service.log";

    private readonly Lock _lock = new();
    private readonly string _logFilePath;

    public FileLogger()
    {
        var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "History", LogDirectoryName);
        Directory.CreateDirectory(directory);
        _logFilePath = Path.Combine(directory, LogFileName);
    }

    public void Log(string message)
    {
        // Logging must never take the service down.
        try
        {
            lock (_lock)
            {
                RotateIfNeeded();
                File.AppendAllText(_logFilePath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}");
            }
        }
        catch { }
    }

    private void RotateIfNeeded()
    {
        var logFileInfo = new FileInfo(_logFilePath);
        if (!logFileInfo.Exists || logFileInfo.Length < MaxLogFileBytes) return;

        var archivedFilePath = _logFilePath + ".1";
        File.Delete(archivedFilePath);
        File.Move(_logFilePath, archivedFilePath);
    }
}
