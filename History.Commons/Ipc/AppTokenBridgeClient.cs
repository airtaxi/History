using System.Diagnostics;
using System.IO.Pipes;

namespace History.Commons.Ipc;

// Client side of the token bridge. Every call is best-effort: when the client is not running (or
// is still starting up) the caller falls back to refreshing the tokens itself.
public sealed class AppTokenBridgeClient
{
    public const string ClientProcessName = "History.WindowsClient";

    public static AppTokenBridgeClient Instance { get; } = new();

    // The client process owns the pipe server; checking for the process avoids a failed pipe
    // connection on every poll cycle while the app is closed.
    public bool IsClientRunning() => Process.GetProcessesByName(ClientProcessName).Length > 0;

    public async Task<TokenBridgeResponse> TrySendAsync(TokenBridgeRequest request, TimeSpan timeout)
    {
        try
        {
            using var cancellationTokenSource = new CancellationTokenSource(timeout);
            await using var pipeClientStream = new NamedPipeClientStream(".", TokenBridgeProtocol.PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);

            await pipeClientStream.ConnectAsync((int)timeout.TotalMilliseconds);
            await TokenBridgeProtocol.WriteMessageAsync(pipeClientStream, request, TokenBridgeJsonSerializerContext.Default.TokenBridgeRequest, cancellationTokenSource.Token);

            return await TokenBridgeProtocol.ReadMessageAsync<TokenBridgeResponse>(pipeClientStream, TokenBridgeJsonSerializerContext.Default.TokenBridgeResponse, cancellationTokenSource.Token);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
