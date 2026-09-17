using History.Commons;
using History.Commons.Api.User;
using History.Commons.Ipc;
using History.Commons.KakaoStory;

namespace History.WindowsClient.Services;

// Answers token requests from the background notification service. The service never refreshes
// the shared tokens while this process is running, because the API server revokes a refresh token
// the instant it is spent and two processes spending it would sign the user out.
public sealed class TokenBridgeService : IAsyncDisposable
{
    private readonly AppTokenBridgeServer _server;

    public TokenBridgeService() => _server = new AppTokenBridgeServer(HandleRequestAsync);

    public void Start() => _server.Start();

    private static async Task<TokenBridgeResponse> HandleRequestAsync(TokenBridgeRequest request, CancellationToken cancellationToken) => request.Kind switch
    {
        TokenBridgeRequestKind.EnsureHistoryTokens => await EnsureHistoryTokensAsync(),
        TokenBridgeRequestKind.EnsureKakaoStoryToken => await EnsureKakaoStoryTokenAsync(),
        _ => new TokenBridgeResponse { IsSuccess = false, ErrorMessage = "Unknown token bridge request." }
    };

    // Runs one authenticated request so the handler refreshes the pair on 401, then returns the
    // pair this process is currently using.
    private static async Task<TokenBridgeResponse> EnsureHistoryTokensAsync()
    {
        if (CommonShared.ApiHandler == ApiHandler.Public) return new TokenBridgeResponse { IsSuccess = false, ErrorMessage = "Signed out." };

        await CommonShared.ApiHandler.TryExecuteRequestAsync(new GetMyProfile());
        return new TokenBridgeResponse { IsSuccess = true, AccessToken = Configuration.GetValue<string>("AccessToken"), RefreshToken = Configuration.GetValue<string>("RefreshToken") };
    }

    private static async Task<TokenBridgeResponse> EnsureKakaoStoryTokenAsync()
    {
        var idToken = await KakaoStoryApiHandler.EnsureKAuthTokenAsync();
        return new TokenBridgeResponse { IsSuccess = idToken != null, KakaoStoryIdToken = idToken, ErrorMessage = idToken == null ? "No Kakao Story session." : null };
    }

    public ValueTask DisposeAsync() => _server.DisposeAsync();
}
