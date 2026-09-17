using History.WindowsClientNotificationService.Core;
using History.WindowsClientNotificationService.Startup;

namespace History.WindowsClientNotificationService;

public static class Program
{
    private static async Task<int> Main(string[] args)
    {
        var logger = new FileLogger();
        try
        {
            // Startup registration runs as a one-shot command issued by the app settings toggle.
            if (await StartupRegistration.TryHandleArgumentsAsync(args, logger)) return 0;

            // Stop runs as a one-shot command that wakes the already running instance.
            if (args.Contains("--try-stop", StringComparer.OrdinalIgnoreCase))
            {
                ServiceStopRequest.Send(logger);
                return 0;
            }

            using var singleInstanceLock = SingleInstanceLock.TryAcquire();
            if (singleInstanceLock == null)
            {
                logger.Log("Another notification service instance is already running; exiting.");
                return 0;
            }

            await using var pollingService = new NotificationPollingService(logger);

            // "--once" performs a single cycle and exits, which is how the service is verified
            // without waiting for a real logon.
            if (args.Contains("--once", StringComparer.OrdinalIgnoreCase))
            {
                await pollingService.RunOnceAsync();
                return 0;
            }

            await pollingService.RunAsync();
            return 0;
        }
        catch (Exception exception)
        {
            logger.Log($"Fatal error: {exception}");
            return 1;
        }
    }
}
