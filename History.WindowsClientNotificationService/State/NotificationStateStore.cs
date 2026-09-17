using System.Text.Json;
using History.WindowsClientNotificationService.Core;

namespace History.WindowsClientNotificationService.State;

// Persists the notification ids the service has already delivered so a restart never re-shows an
// old notification. Each mutation writes the file immediately; the lists are small and the poll
// cadence is measured in seconds.
public sealed class NotificationStateStore
{
    private const int MaxKnownNotificationIds = 200;
    private const string StateFileName = "notification-service-state.json";

    private readonly Lock _lock = new();
    private readonly FileLogger _logger;
    private readonly string _stateFilePath;
    private NotificationServiceState _state;

    public NotificationStateStore(FileLogger logger)
    {
        _logger = logger;
        var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "History");
        _stateFilePath = Path.Combine(directory, StateFileName);
        _state = Load();
    }

    public bool IsHistoryInitialized
    {
        get { lock (_lock) return _state.IsHistoryInitialized; }
    }

    public bool IsKakaoStoryInitialized
    {
        get { lock (_lock) return _state.IsKakaoStoryInitialized; }
    }

    public bool IsHistoryNotificationKnown(string id)
    {
        lock (_lock) return _state.HistoryKnownNotificationIds.Contains(id);
    }

    public bool IsKakaoStoryNotificationKnown(string id)
    {
        lock (_lock) return _state.KakaoStoryKnownNotificationIds.Contains(id);
    }

    public void MarkHistoryInitialized()
    {
        lock (_lock)
        {
            _state.IsHistoryInitialized = true;
            SaveLocked();
        }
    }

    public void MarkKakaoStoryInitialized()
    {
        lock (_lock)
        {
            _state.IsKakaoStoryInitialized = true;
            SaveLocked();
        }
    }

    public void RecordHistoryNotifications(IEnumerable<string> ids)
    {
        lock (_lock)
        {
            _state.HistoryKnownNotificationIds = MergeIds(_state.HistoryKnownNotificationIds, ids);
            SaveLocked();
        }
    }

    public void RecordKakaoStoryNotifications(IEnumerable<string> ids)
    {
        lock (_lock)
        {
            _state.KakaoStoryKnownNotificationIds = MergeIds(_state.KakaoStoryKnownNotificationIds, ids);
            SaveLocked();
        }
    }

    public void Save()
    {
        lock (_lock)
        {
            SaveLocked();
        }
    }

    // The freshly fetched ids lead the list so the window always keeps the newest entries.
    private static List<string> MergeIds(List<string> knownIds, IEnumerable<string> ids)
    {
        var mergedIds = new List<string>(ids.Where(id => id != null));
        mergedIds.AddRange(knownIds.Where(id => !mergedIds.Contains(id)));
        return mergedIds.Take(MaxKnownNotificationIds).ToList();
    }

    private NotificationServiceState Load()
    {
        try
        {
            if (!File.Exists(_stateFilePath)) return new NotificationServiceState();
            return JsonSerializer.Deserialize<NotificationServiceState>(File.ReadAllText(_stateFilePath)) ?? new NotificationServiceState();
        }
        catch (Exception exception)
        {
            _logger.Log($"Notification state load failed; starting fresh: {exception.Message}");
            return new NotificationServiceState();
        }
    }

    private void SaveLocked()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_stateFilePath));
            File.WriteAllText(_stateFilePath, JsonSerializer.Serialize(_state, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception exception) { _logger.Log($"Notification state save failed: {exception.Message}"); }
    }
}
