using System.Diagnostics;
using UIKit;

namespace History.MobileClient;

public class Program
{
    // This is the main entry point of the application.
    static void Main(string[] args)
    {
        // if you want to use a different Application Delegate class from "AppDelegate"
        // you can specify it here.
        try { UIApplication.Main(args, null, typeof(AppDelegate)); }
        catch (Exception exception)
        {
            var message = exception.Message ?? "Unknown error";
            var stackTrace = exception.StackTrace ?? "No stack trace available";
            Debug.WriteLine($"Exception in Main: {message} / {stackTrace}");
        }
    }
}
