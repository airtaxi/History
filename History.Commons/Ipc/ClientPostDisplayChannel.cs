using History.Commons.Enums;
using System.IO.MemoryMappedFiles;
using System.Runtime.Versioning;

namespace History.Commons.Ipc;

// Shared memory and signal that carry the post the client currently shows to the background
// notification service, and carry that service's refresh request back to the client. The client
// publishes its display state, and the notification service reads it to suppress a duplicate
// toast and writes the post id it wants reloaded before setting the named event. Both processes
// create or open the same map, so either side may start first, and every access is best-effort
// because the peer can exit at any moment.
[SupportedOSPlatform("windows")]
public sealed class ClientPostDisplayChannel : IDisposable
{
    private const int IsForegroundTrue = 1;

    // The accessor is not documented as thread safe, and the two polling loops plus the client's
    // UI thread reach this channel concurrently, so every accessor touch is serialized here.
    private readonly Lock _accessorLock = new();
    private readonly MemoryMappedFile _memoryMappedFile;
    private readonly MemoryMappedViewAccessor _accessor;
    private readonly EventWaitHandle _refreshEvent;

    private ClientPostDisplayChannel(MemoryMappedFile memoryMappedFile, MemoryMappedViewAccessor accessor, EventWaitHandle refreshEvent)
    {
        _memoryMappedFile = memoryMappedFile;
        _accessor = accessor;
        _refreshEvent = refreshEvent;
    }

    // The named event the client waits on; null when the event could not be created, in which case
    // the display state still works but the refresh signal is unavailable.
    public WaitHandle RefreshWaitHandle => _refreshEvent;

    public static ClientPostDisplayChannel TryCreate()
    {
        MemoryMappedFile memoryMappedFile = null;
        try
        {
            memoryMappedFile = MemoryMappedFile.CreateOrOpen(ClientPostDisplayProtocol.MemoryMappedFileName, ClientPostDisplayProtocol.MemorySize, MemoryMappedFileAccess.ReadWrite);

            var accessor = memoryMappedFile.CreateViewAccessor();
            var refreshEvent = TryCreateRefreshEvent();
            if (accessor.ReadInt32(ClientPostDisplayProtocol.ProtocolVersionOffset) != ClientPostDisplayProtocol.ProtocolVersion) accessor.Write(ClientPostDisplayProtocol.ProtocolVersionOffset, ClientPostDisplayProtocol.ProtocolVersion);

            return new ClientPostDisplayChannel(memoryMappedFile, accessor, refreshEvent);
        }
        catch
        {
            memoryMappedFile?.Dispose();
            return null;
        }
    }

    public void PublishDisplayedPost(NotificationPostPlatform platform, string postId)
    {
        lock (_accessorLock)
        {
            WritePostId(ClientPostDisplayProtocol.DisplayedPostIdOffset, ClientPostDisplayProtocol.DisplayedPostIdLengthOffset, postId);

            // The platform value is published last so a reader that observes it also observes the
            // matching post id.
            Thread.MemoryBarrier();
            _accessor.Write(ClientPostDisplayProtocol.DisplayedPlatformOffset, (int)platform);
        }
    }

    public void ClearDisplayedPost()
    {
        lock (_accessorLock)
        {
            _accessor.Write(ClientPostDisplayProtocol.DisplayedPlatformOffset, (int)NotificationPostPlatform.None);
        }
    }

    public void PublishIsForeground(bool isForeground)
    {
        lock (_accessorLock)
        {
            _accessor.Write(ClientPostDisplayProtocol.IsForegroundOffset, isForeground ? IsForegroundTrue : 0);
        }
    }

    public bool TryGetDisplayedPost(out NotificationPostPlatform platform, out string postId, out bool isForeground)
    {
        lock (_accessorLock)
        {
            platform = NotificationPostPlatform.None;
            postId = null;
            isForeground = false;

            if (_accessor.ReadInt32(ClientPostDisplayProtocol.ProtocolVersionOffset) != ClientPostDisplayProtocol.ProtocolVersion) return false;

            var platformValue = _accessor.ReadInt32(ClientPostDisplayProtocol.DisplayedPlatformOffset);
            if (platformValue == (int)NotificationPostPlatform.None) return false;

            postId = ReadPostId(ClientPostDisplayProtocol.DisplayedPostIdOffset, ClientPostDisplayProtocol.DisplayedPostIdLengthOffset);
            if (string.IsNullOrEmpty(postId)) return false;

            platform = (NotificationPostPlatform)platformValue;
            isForeground = _accessor.ReadInt32(ClientPostDisplayProtocol.IsForegroundOffset) == IsForegroundTrue;
            return true;
        }
    }

    public void RequestPostRefresh(NotificationPostPlatform platform, string postId)
    {
        lock (_accessorLock)
        {
            WritePostId(ClientPostDisplayProtocol.RefreshPostIdOffset, ClientPostDisplayProtocol.RefreshPostIdLengthOffset, postId);

            // The platform value is published last so a reader that observes it also observes the
            // matching post id.
            Thread.MemoryBarrier();
            _accessor.Write(ClientPostDisplayProtocol.RefreshPlatformOffset, (int)platform);
        }
    }

    public bool TryGetRefreshRequest(out NotificationPostPlatform platform, out string postId)
    {
        lock (_accessorLock)
        {
            platform = NotificationPostPlatform.None;
            postId = null;

            if (_accessor.ReadInt32(ClientPostDisplayProtocol.ProtocolVersionOffset) != ClientPostDisplayProtocol.ProtocolVersion) return false;

            var platformValue = _accessor.ReadInt32(ClientPostDisplayProtocol.RefreshPlatformOffset);
            if (platformValue == (int)NotificationPostPlatform.None) return false;

            postId = ReadPostId(ClientPostDisplayProtocol.RefreshPostIdOffset, ClientPostDisplayProtocol.RefreshPostIdLengthOffset);
            if (string.IsNullOrEmpty(postId)) return false;

            platform = (NotificationPostPlatform)platformValue;
            return true;
        }
    }

    public void SignalPostRefresh() => _refreshEvent?.Set();

    public void ResetPostRefreshSignal() => _refreshEvent?.Reset();

    public void Dispose()
    {
        lock (_accessorLock)
        {
            _accessor.Dispose();
            _refreshEvent?.Dispose();
            _memoryMappedFile.Dispose();
        }
    }

    private static EventWaitHandle TryCreateRefreshEvent()
    {
        try { return new EventWaitHandle(false, EventResetMode.AutoReset, ClientPostDisplayProtocol.PostRefreshEventName); }
        catch { return null; }
    }

    private void WritePostId(int postIdOffset, int postIdLengthOffset, string postId)
    {
        var characters = (postId ?? string.Empty).ToCharArray();
        var length = Math.Min(characters.Length, ClientPostDisplayProtocol.MaxPostIdLength);
        _accessor.WriteArray(postIdOffset, characters, 0, length);

        // The length is published after the buffer so a reader never pairs a new length with a
        // partially written id.
        Thread.MemoryBarrier();
        _accessor.Write(postIdLengthOffset, length);
    }

    private string ReadPostId(int postIdOffset, int postIdLengthOffset)
    {
        var length = _accessor.ReadInt32(postIdLengthOffset);
        if (length <= 0 || length > ClientPostDisplayProtocol.MaxPostIdLength) return null;

        var characters = new char[length];
        _accessor.ReadArray(postIdOffset, characters, 0, length);
        return new string(characters);
    }
}
