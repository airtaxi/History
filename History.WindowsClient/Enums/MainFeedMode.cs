namespace History.WindowsClient.Enums;

// Which feed the main frame is showing. The title bar toggles are all derived from
// this single value, so exactly one feed can be active at a time.
public enum MainFeedMode
{
    Timeline,
    Discover,
    Bookmarks
}
