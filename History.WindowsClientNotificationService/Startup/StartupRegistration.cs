using History.Commons.Ipc;
using History.WindowsClientNotificationService.Core;
using Microsoft.Win32;
using Windows.ApplicationModel;

namespace History.WindowsClientNotificationService.Startup;

// Owns the logon startup registration. The MSIX startup task is preferred because the package
// tracks the executable path across updates and Task Manager gives the user a toggle; the HKCU
// Run value is a development fallback for running the service outside an installed package.
public static class StartupRegistration
{
    private const string ArgumentRegister = "--try-register";
    private const string ArgumentUnregister = "--try-unregister";
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValueName = "History.WindowsClientNotificationService";

    public static async Task<bool> TryHandleArgumentsAsync(string[] args, FileLogger logger)
    {
        if (args.Length == 0) return false;

        var argument = args[0].ToLowerInvariant();
        if (argument == ArgumentRegister)
        {
            await RegisterAsync(logger);
            return true;
        }

        if (argument == ArgumentUnregister)
        {
            await UnregisterAsync(logger);
            return true;
        }

        return false;
    }

    private static async Task RegisterAsync(FileLogger logger)
    {
        var startupTask = await TryGetStartupTaskAsync();
        if (startupTask != null)
        {
            logger.Log($"Startup task enable requested. Result state: {await startupTask.RequestEnableAsync()}");
            return;
        }

        var executablePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(executablePath))
        {
            logger.Log("Startup registration failed: the executable path is unavailable.");
            return;
        }

        if (executablePath.Contains(@"\WindowsApps\", StringComparison.OrdinalIgnoreCase)) logger.Log("Warning: a HKCU Run entry pointing inside WindowsApps breaks on the next package update; prefer the startup task.");

        using var runKey = Registry.CurrentUser.CreateSubKey(RunKeyPath);
        runKey?.SetValue(RunValueName, $"\"{executablePath}\"");
        logger.Log($"Startup registration written to HKCU Run: {executablePath}");
    }

    private static async Task UnregisterAsync(FileLogger logger)
    {
        var startupTask = await TryGetStartupTaskAsync();
        if (startupTask != null)
        {
            startupTask.Disable();
            logger.Log($"Startup task disabled. Result state: {startupTask.State}");
            return;
        }

        using var runKey = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        if (runKey?.GetValue(RunValueName) == null)
        {
            logger.Log("Startup unregistration skipped: no HKCU Run entry exists.");
            return;
        }

        runKey.DeleteValue(RunValueName);
        logger.Log("Startup registration removed from HKCU Run.");
    }

    // Returns null when the process has no package identity or the package does not declare the
    // startup task, which is exactly when the HKCU Run fallback applies.
    private static async Task<StartupTask> TryGetStartupTaskAsync()
    {
        try { return await StartupTask.GetAsync(NotificationServiceProtocol.TaskId); }
        catch (Exception) { return null; }
    }
}
