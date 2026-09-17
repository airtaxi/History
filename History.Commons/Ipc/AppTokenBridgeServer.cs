using System.IO.Pipes;

namespace History.Commons.Ipc;

// Hosts the token bridge pipe inside the running client process. The background notification
// service asks the client to refresh the shared tokens instead of refreshing them itself,
// because the server revokes a refresh token the instant it is spent and only one process may
// spend it.
public sealed class AppTokenBridgeServer : IAsyncDisposable
{
    private readonly Func<TokenBridgeRequest, CancellationToken, Task<TokenBridgeResponse>> _handler;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private Task _acceptLoopTask;

    public AppTokenBridgeServer(Func<TokenBridgeRequest, CancellationToken, Task<TokenBridgeResponse>> handler) => _handler = handler;

    public void Start() => _acceptLoopTask = Task.Run(AcceptLoopAsync);

    private async Task AcceptLoopAsync()
    {
        while (!_cancellationTokenSource.IsCancellationRequested)
        {
            try
            {
                await using var pipeServerStream = new NamedPipeServerStream(TokenBridgeProtocol.PipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                await pipeServerStream.WaitForConnectionAsync(_cancellationTokenSource.Token);

                var request = await TokenBridgeProtocol.ReadMessageAsync<TokenBridgeRequest>(pipeServerStream, _cancellationTokenSource.Token);
                var response = request == null ? InvalidRequestResponse() : await _handler(request, _cancellationTokenSource.Token);

                await TokenBridgeProtocol.WriteMessageAsync(pipeServerStream, response, _cancellationTokenSource.Token);
            }
            catch (OperationCanceledException) { return; }
            catch
            {
                // A failed connection must not kill the bridge; the next request reconnects after
                // the loop creates a fresh pipe instance.
            }
        }
    }

    private static TokenBridgeResponse InvalidRequestResponse() => new() { IsSuccess = false, ErrorMessage = "Invalid token bridge request." };

    public async ValueTask DisposeAsync()
    {
        await _cancellationTokenSource.CancelAsync();
        if (_acceptLoopTask != null)
        {
            try { await _acceptLoopTask; }
            catch { }
        }

        _cancellationTokenSource.Dispose();
    }
}
