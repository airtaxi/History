using System.Diagnostics;
using History.Commons.Ipc;
using Windows.ApplicationModel;

namespace History.WindowsClient.Services;

// Starts and stops the background notification service process that ships inside this package.
// The logon startup task keeps it running across sessions; this helper covers the moment the user
// turns the setting on and the moment the app comes up with the task already enabled.
public static class BackgroundNotificationServiceController
{
    public static bool IsRunning() => Process.GetProcessesByName(NotificationServiceProtocol.ProcessName).Length > 0;

    public static async Task EnsureRunningIfEnabledAsync()
    {
        try
        {
            var startupTask = await StartupTask.GetAsync(NotificationServiceProtocol.TaskId);
            if (startupTask.State == StartupTaskState.Enabled) TryStart();
        }
        // A build without the declared startup task simply leaves the service alone.
        catch { }
    }

    public static bool TryStart()
    {
        if (IsRunning()) return true;

        var executablePath = ResolveExecutablePath();
        if (executablePath == null) return false;

        try
        {
            Process.Start(new ProcessStartInfo(executablePath) { UseShellExecute = false, WorkingDirectory = AppContext.BaseDirectory });
            return true;
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"Background notification service start failed: {exception.Message}");
            return false;
        }
    }

    // Signals the service to leave its poll loop, then terminates it when it does not exit in time.
    public static async Task StopAsync()
    {
        var executablePath = ResolveExecutablePath();
        if (executablePath != null) TrySignalStop(executablePath);

        for (var attempt = 0; attempt < 6; attempt++)
        {
            if (!IsRunning()) return;
            await Task.Delay(500);
        }

        foreach (var process in Process.GetProcessesByName(NotificationServiceProtocol.ProcessName))
        {
            try { process.Kill(); }
            catch { }
        }
    }

    private static void TrySignalStop(string executablePath)
    {
        try { Process.Start(new ProcessStartInfo(executablePath, "--try-stop") { UseShellExecute = false }); }
        catch { }
    }

    private static string ResolveExecutablePath()
    {
        var executablePath = Path.Combine(AppContext.BaseDirectory, NotificationServiceProtocol.ExecutableName);
        return File.Exists(executablePath) ? executablePath : null;
    }
}
