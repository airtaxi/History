namespace History.Commons.Ipc;

// Request kinds exchanged over the token bridge between the running Windows client (server) and
// the background notification service (client).
public enum TokenBridgeRequestKind
{
    EnsureHistoryTokens,
    EnsureKakaoStoryToken
}
