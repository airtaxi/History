using History.Commons.Ipc;

namespace History.WindowsClientNotificationService.Core;

// Signals a running service instance to shut down, which is how the app turns the background
// notification setting off. The service exits between poll cycles instead of being terminated.
public static class ServiceStopRequest
{
    public static bool Send(FileLogger logger)
    {
        try
        {
            using var stopEvent = EventWaitHandle.OpenExisting(NotificationServiceProtocol.StopEventName);
            stopEvent.Set();
            logger.Log("Stop signal sent to the running service instance.");
            return true;
        }
        catch (Exception exception)
        {
            logger.Log($"Stop signal failed: {exception.Message}");
            return false;
        }
    }
}
